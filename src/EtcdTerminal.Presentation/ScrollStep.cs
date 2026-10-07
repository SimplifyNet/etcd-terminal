namespace EtcdTerminal.Presentation;

/// <summary>
/// One step of a page's body through the viewport between the fixed header
/// and the fixed hint. The semantic distance is chosen by the renderer: a
/// line is a line, a page is the rows of the viewport.
/// </summary>
public enum ScrollStep
{
	LineUp,
	LineDown,
	PageUp,
	PageDown,
	Top,
	Bottom
}
