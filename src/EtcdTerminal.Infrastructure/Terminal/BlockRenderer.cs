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

	/// No column of a headered list is wider than this, so the columns stand
	/// close together instead of spreading across the region with canyons of
	/// empty space between them. It is a layout preference, not a
	/// measurement, so the model stays width-free.
	private const int ListColumnWidth = 50;

	private Color BandColor => new(_theme.BandBackground.R, _theme.BandBackground.G, _theme.BandBackground.B);

	/// The banner is centered across the whole width and is not content, so
	/// it keeps no left margin; everything else starts on the fourth column.
	/// A banded line keeps the band's own second column inside its content
	/// row while the background runs edge to edge with a background row above
	/// and below — the status bar's treatment, shared by the filter and
	/// pagination. A row that carries the selection pointer starts on the
	/// second column instead: the pointer takes the margin and the cell after
	/// it lands on the fourth, where every other row's text sits.
	public IRenderable Render(Block block) =>
		block switch
		{
			BannerBlock => Content(block),
			ActionPanelBlock => new BackgroundBand(
				Content(block),
				new BandStyle(BandColor, TrailingBreak: true)
				{
					StripeStyle = _styles.Resolve(TextRole.Accent),
					StripeOffset = 0,
					ContentIndent = 2,
					LineBackgrounds = ActionPanelBackgrounds(),
					VerticalPadding = false
				}),
			TextBlock { Band: true } => new BackgroundBand(Indented(block, ContentIndent.BandColumns), new BandStyle(BandColor, TrailingBreak: true)),
			TableBlock { Pointer: true } => Indented(block, ContentIndent.MarkerColumns),
			_ => Indented(block, ContentIndent.Columns)
		};

	private IRenderable Indented(Block block, int columns) =>
		new Padder(Content(block), new Padding(columns, 0, 0, 0));

	private IRenderable Content(Block block) =>
		block switch
		{
			ActionPanelBlock panel => new Rows(
			[
				_styles.Build([]),
				_styles.Build(panel.Title),
				_styles.Build([]),
				_styles.Build([]),
				_styles.Build(panel.Actions),
				_styles.Build([])
			]),
			TextBlock text => new Rows(text.Lines.Select(_styles.Build)),
			TitleBlock title => new Padder(_styles.Build([title.Title]), new Padding(0, TitleSpacingAbove, 0, 0)),
			BannerBlock banner => Banner(banner),
			TableBlock table => RenderTable(table),
			_ => new Rows()
		};

	private Color SelectionBackground => new(_theme.ActionPanelTitleBackground.R, _theme.ActionPanelTitleBackground.G, _theme.ActionPanelTitleBackground.B);

	private IReadOnlyList<Color> ActionPanelBackgrounds() =>
		[
			SelectionBackground,
			SelectionBackground,
			SelectionBackground,
			BandColor,
			BandColor
		];

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
	/// table stays borderless, fills the region and hands its columns to
	/// <see cref="Halved"/>, which decides the column widths. A table with a
	/// header gets one blank row under it so the accent heading does not
	/// merge into the first line of data.
	/// </summary>
	private IRenderable RenderTable(TableBlock block) =>
		block.IsFramed ? BuildTable(block, []) : new Halved(this, block);

	private IRenderable BuildTable(TableBlock block, int?[] widths)
	{
		var columns = Math.Max(block.Header.Count, ColumnCount(block));
		var table = block.IsFramed ? new Table() : new Table().NoBorder().Expand();

		table.ShowHeaders = block.Header.Count > 0;

		for (var column = 0; column < columns; column++)
			table.AddColumn(new TableColumn(Header(block, column))
			{
				NoWrap = true,
				Padding = block.IsFramed ? null : new Padding(0, 0, 0, 0),
				Width = ColumnWidth(block, column, widths)
			});

		if (block.Header.Count > 0)
			table.AddRow(Cells([], columns));

		foreach (var row in block.Rows)
			table.AddRow(Cells(row, columns));

		return table;
	}

	/// The split widths belong to the content columns; a pointer table keeps
	/// its first column on the margin's width so the split still lands on the
	/// center line. A framed table passes no widths and lets Spectre measure.
	private static int? ColumnWidth(TableBlock block, int column, int?[] widths) =>
		(block.Pointer, column) switch
		{
			(true, 0) => ContentIndent.MarkerColumns,
			_ when column < widths.Length => widths[column],
			_ => null
		};

	/// The header goes through the same role mapping as the cells: built
	/// from a bare string, Spectre would draw it in the default color and
	/// drop the role the model gave it.
	private Paragraph Header(TableBlock block, int column)
	{
		IReadOnlyList<StyledText> spans = column < block.Header.Count ? [block.Header[column]] : [];
		var paragraph = _styles.Build(spans);

		paragraph.Overflow = Overflow.Ellipsis;

		return paragraph;
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
	/// The borderless table's columns split the region into equal shares —
	/// halves for two columns, thirds for three — instead of handing the
	/// spare width to the column Spectre measured wider, so a list stays
	/// balanced the way the terminal before the migration cut every line in
	/// two halves did. A table with a header is a list page and its columns
	/// stand close together: no column grows past <see cref="ListColumnWidth"/>
	/// columns, and when the region is too narrow for that every column
	/// shrinks by the same share. The split happens at render time from the
	/// width the parent hands in, so it follows a terminal resize and the
	/// model never measures anything. A cell wider than its column is cropped
	/// by the ellipsis, the way the old TruncateText did. The last column
	/// takes the rounding remainder so the shares add up to the region
	/// exactly. The borderless columns carry a zero padding on purpose:
	/// BoxBorder.None still reports UsePadding, so the measurer would budget
	/// pad cells the renderer never draws and Ratio.Reduce would shrink a
	/// column off its share.
	/// </summary>
	private sealed class Halved(BlockRenderer _owner, TableBlock _block) : Renderable
	{
		protected override Measurement Measure(RenderOptions options, int maxWidth) =>
			Table(maxWidth).Measure(options, maxWidth);

		protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
			Table(maxWidth).Render(options, maxWidth);

		/// A pointer column keeps its margin width out of the split, so the
		/// remaining columns share the region to the right of it and still
		/// meet on the center line.
		private IRenderable Table(int maxWidth)
		{
			var columns = Math.Max(_block.Header.Count, ColumnCount(_block));
			var start = _block.Pointer ? 1 : 0;
			var share = columns - start;

			if (share <= 0)
				return _owner.BuildTable(_block, []);

			var content = Math.Max(0, maxWidth - (_block.Pointer ? ContentIndent.MarkerColumns : 0));
			var width = content / share;
			int?[] widths = new int?[columns];

			for (var column = start; column < columns; column++)
			{
				var columnWidth = column == columns - 1 ? content - width * (share - 1) : width;

				widths[column] = CapsColumns ? Math.Min(columnWidth, ListColumnWidth) : columnWidth;
			}

			return _owner.BuildTable(_block, widths);
		}

		private bool CapsColumns => !_block.Pointer && _block.Header.Count > 0;
	}
}
