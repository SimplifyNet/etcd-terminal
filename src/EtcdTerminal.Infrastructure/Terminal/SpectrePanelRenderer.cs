using EtcdTerminal.Presentation;
using EtcdTerminal.Theming;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// Renders panel content as a borderless full-width block. Gutter, vertical
/// spacing, column widths and line composition are decided here, never in the
/// component that builds the model.
/// </summary>
public sealed class SpectrePanelRenderer(RoleStyleMapper _styles, ITheme _theme) : IPanelRenderer
{
	// Padding takes (horizontal, vertical). The selection marker inside the
	// model already carries the indentation, so the horizontal value is only a
	// gutter; the vertical value separates the block from the banner and footer.
	private const int HorizontalSpacing = 1;
	private const int VerticalSpacing = 2;

	public void Write(PanelModel panel) =>
		AnsiConsole.Write(Build(panel, AnsiConsole.Profile.Width));

	public Panel Build(PanelModel panel, int width) => new Panel(Content(panel, width))
		.NoBorder()
		.Padding(HorizontalSpacing, VerticalSpacing);

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
		new Rows([.. panel.Lines.Select(line => _styles.Build(line.Spans))]);
}
