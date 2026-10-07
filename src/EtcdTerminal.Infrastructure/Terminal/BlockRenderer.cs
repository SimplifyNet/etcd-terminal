using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Theming;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// The single mapper from a content block to a Spectre renderable. Widgets do
/// the measuring, wrapping, cropping and alignment; this class only picks the
/// widget and hands it the literal text and the semantic styles.
/// </summary>
public sealed class BlockRenderer(RoleStyleMapper _styles, ITheme _theme)
{
	/// A section title hugs the table it introduces; a title has no frame of
	/// its own, so the single blank line above it is what separates it from
	/// the previous section.
	private const int TitleSpacingAbove = 1;

	private Color BandColor => new(_theme.BandBackground.R, _theme.BandBackground.G, _theme.BandBackground.B);

	/// The banner is centered across the whole width and is not content, so
	/// it keeps no left margin; everything else starts on the fourth column.
	/// A banded line keeps the band's own second column inside its content
	/// row while the background runs edge to edge with a background row above
	/// and below — the status bar's treatment, shared by the filter and
	/// pagination.
	public IRenderable Render(Block block) =>
		block switch
		{
			BannerBlock => Content(block),
			TextBlock { Band: true } => new BackgroundBand(Indented(block, ContentIndent.BandColumns), BandColor, _trailingBreak: true),
			_ => Indented(block, ContentIndent.Columns)
		};

	private IRenderable Indented(Block block, int columns) =>
		new Padder(Content(block), new Padding(columns, 0, 0, 0));

	private IRenderable Content(Block block) =>
		block switch
		{
			TextBlock text => new Rows(text.Lines.Select(_styles.Build)),
			TitleBlock title => new Padder(_styles.Build([title.Title]), new Padding(0, TitleSpacingAbove, 0, 0)),
			BannerBlock banner => Banner(banner),
			TableBlock table => RenderTable(table),
			_ => new Rows()
		};

	/// <summary>
	/// Renders a title banner. The model carries the text; the widget, its
	/// centering and its colour are Infrastructure's decision.
	/// </summary>
	private IRenderable Banner(BannerBlock block)
	{
		var color = _theme.Banner;

		return new FigletText(block.Text).Color(new Color(color.R, color.G, color.B)).Centered();
	}

	/// <summary>
	/// Lets Spectre size the columns and crop every cell to the available
	/// width. The application never measures the text it put in the model.
	/// A framed table keeps Spectre's default border at its natural content
	/// width — the style the old terminal's WriteTable drew; every other
	/// table stays borderless, fills the region and hands its two columns to
	/// <see cref="Halved"/>, which cuts the region in half.
	/// </summary>
	private IRenderable RenderTable(TableBlock block) =>
		block.IsFramed ? BuildTable(block, null, null) : new Halved(this, block);

	private IRenderable BuildTable(TableBlock block, int? keyWidth, int? valueWidth)
	{
		var columns = Math.Max(block.Header.Count, ColumnCount(block));
		var table = block.IsFramed ? new Table() : new Table().NoBorder().Expand();

		table.ShowHeaders = block.Header.Count > 0;

		for (var column = 0; column < columns; column++)
			table.AddColumn(new TableColumn(column < block.Header.Count ? Markup.Escape(block.Header[column].Text) : string.Empty)
			{
				NoWrap = true,
				Padding = block.IsFramed ? null : new Padding(0, 0, 0, 0),
				Width = column switch { 0 => keyWidth, 1 => valueWidth, _ => null }
			});

		foreach (var row in block.Rows)
			table.AddRow(Cells(row, columns));

		return table;
	}

	private IRenderable[] Cells(IReadOnlyList<StyledText> row, int columns)
	{
		IRenderable[] cells = new IRenderable[columns];

		for (var column = 0; column < columns; column++)
		{
			IReadOnlyList<StyledText> spans = column < row.Count ? [row[column]] : [];
			var paragraph = _styles.Build(spans);

			paragraph.Overflow = Overflow.Ellipsis;
			cells[column] = paragraph;
		}

		return cells;
	}

	private static int ColumnCount(TableBlock block) =>
		block.Rows.Count == 0 ? 0 : block.Rows.Max(row => row.Count);

	/// <summary>
	/// The borderless table's two columns split the region in half. Spectre
	/// hands its spare width to the wider column (Ratio.Distribute follows
	/// the measured widths), so a long key column keeps growing and the
	/// values start right of the center; the terminal before the migration
	/// cut every line in two halves instead (key column = width / 2), and
	/// this keeps that balance. The split happens at render time from the
	/// width the parent hands in, so it follows a terminal resize and the
	/// model never measures anything. A cell wider than its half is cropped
	/// by the column's ellipsis, the way the old TruncateText did. The
	/// borderless columns carry a zero padding on purpose: BoxBorder.None
	/// still reports UsePadding, so the measurer would budget pad cells the
	/// renderer never draws and Ratio.Reduce would shrink the key column
	/// off the center.
	/// </summary>
	private sealed class Halved(BlockRenderer _owner, TableBlock _block) : Renderable
	{
		protected override Measurement Measure(RenderOptions options, int maxWidth) =>
			_owner.BuildTable(_block, maxWidth / 2, maxWidth - maxWidth / 2).Measure(options, maxWidth);

		protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
			_owner.BuildTable(_block, maxWidth / 2, maxWidth - maxWidth / 2).Render(options, maxWidth);
	}
}
