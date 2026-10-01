using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Theming;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// Renders panel content as a borderless full-width block. Gutter, vertical
/// spacing, column widths and line composition are decided here, never in the
/// component that builds the model.
/// </summary>
public sealed class SpectrePanelRenderer(IAnsiConsole _console, RoleStyleMapper _styles, ITheme _theme) : IPanelRenderer
{
	// Content starts at column zero. The selection marker or the terminal
	// indent inside the model is what moves a line in, exactly as the inline
	// menu and the inline details wrote them before the migration, so the panel
	// must not add a column of its own on the left. The right edge keeps one
	// column of its own, the same column the inline truncation reserved, so
	// text never runs into it.
	private const int PaddingRight = 1;

	// Spectre's grid leaves this much room between neighbouring columns.
	private const int ColumnGap = 2;

	// The first cell of a table row is the selection marker. Spectre measures
	// it so that keys and values keep their own columns.
	private const int MarkerColumns = 1;

	// A section title is separated from what surrounds it by a blank line on
	// each side. Borders are gone, so those lines are what keep one section
	// from running into the next.
	private const int TitleSpacingAbove = 1;
	private const int TitleSpacingBelow = 1;

	public void Write(PanelModel panel) =>
		_console.Write(Build(panel, _console.Profile.Width));

	public Panel Build(PanelModel panel, int width) => new Panel(Content(panel, width))
		.NoBorder()
		.Padding(Spacing(panel));

	private static Padding Spacing(PanelModel panel) =>
		panel.Kind switch
		{
			PanelKind.Title => new Padding(0, TitleSpacingAbove, PaddingRight, TitleSpacingBelow),
			_ => new Padding(0, 0, PaddingRight, 0)
		};

	/// <summary>
	/// Renders a title banner. The model carries the text; the widget, its
	/// centering and its colour are Infrastructure's decision.
	/// </summary>
	public IRenderable Banner(PanelModel panel)
	{
		var text = panel.Lines.Count > 0 ? panel.Lines[0].Text : string.Empty;
		var color = _theme.Banner;

		return new FigletText(text).Color(new Color(color.R, color.G, color.B)).Centered();
	}

	private IRenderable Content(PanelModel panel, int width) =>
		panel.Kind is PanelKind.Table && Columns(panel) > 1
			? Table(panel, width)
			: Rows(panel);

	private IRenderable Rows(PanelModel panel) =>
		new Rows([.. panel.Lines.Select(line => Line(line.Spans))]);

	/// <summary>
	/// Lays a table panel out as equal content columns behind a marker column
	/// that Spectre sizes itself, and lets Spectre crop every cell to one line.
	/// The application never measures the text it put in the model.
	/// </summary>
	private IRenderable Table(PanelModel panel, int width)
	{
		var columns = Columns(panel);
		var contentWidth = Math.Max(1, width - PaddingRight);
		var contentColumns = Math.Max(1, columns - MarkerColumns);
		var cellWidth = Math.Max(1, (contentWidth - ColumnGap * columns) / contentColumns);

		var grid = new Grid { Width = contentWidth, Expand = true };

		for (var column = 0; column < columns; column++)
			grid.AddColumn(new GridColumn { Width = column < MarkerColumns ? null : cellWidth });

		foreach (var line in panel.Lines)
			grid.AddRow(Cells(line, columns));

		return grid;
	}

	private IRenderable[] Cells(PanelLine line, int columns)
	{
		IRenderable[] cells = new IRenderable[columns];

		for (var column = 0; column < columns; column++)
		{
			IReadOnlyList<StyledText> spans = column < line.Spans.Count ? [line.Spans[column]] : [];
			var paragraph = _styles.Build(spans);

			paragraph.Overflow = Overflow.Crop;
			cells[column] = paragraph;
		}

		return cells;
	}

	private Paragraph Line(IReadOnlyList<StyledText> spans) => _styles.Build(spans);

	private static int Columns(PanelModel panel) =>
		panel.Lines.Count == 0 ? 0 : panel.Lines.Max(line => line.Spans.Count);
}
