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
	/// A section title is separated from what surrounds it by a blank line on
	/// each side. Borders are gone, so those lines are what keep one section
	/// from running into the next.
	private const int TitleSpacingAbove = 1;
	private const int TitleSpacingBelow = 1;

	/// The banner is centered across the whole width and is not content, so
	/// it keeps no left margin; everything else starts on the fourth column.
	public IRenderable Render(Block block) =>
		block is BannerBlock
			? Content(block)
			: new Padder(Content(block), new Padding(ContentIndent.Columns, 0, 0, 0));

	private IRenderable Content(Block block) =>
		block switch
		{
			TextBlock text => new Rows(text.Lines.Select(_styles.Build)),
			TitleBlock title => new Padder(_styles.Build([title.Title]), new Padding(0, TitleSpacingAbove, 0, TitleSpacingBelow)),
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
	/// </summary>
	private IRenderable RenderTable(TableBlock block)
	{
		var columns = Math.Max(block.Header.Count, ColumnCount(block));
		var table = new Table().NoBorder().Expand();

		table.ShowHeaders = block.Header.Count > 0;

		for (var column = 0; column < columns; column++)
			table.AddColumn(new TableColumn(column < block.Header.Count ? Markup.Escape(block.Header[column].Text) : string.Empty)
			{
				NoWrap = true
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
}
