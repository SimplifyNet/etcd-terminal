namespace EtcdTerminal.Presentation;

/// <summary>
/// One logical line of a panel, split into styled runs so that labels, values
/// and separators can carry different roles.
/// </summary>
public sealed record PanelLine(IReadOnlyList<StyledText> Spans)
{
	/// <summary>
	/// The line as literal text, without roles. Used for measurement and for
	/// asserting content, never for rendering.
	/// </summary>
	public string Text => string.Concat(Spans.Select(span => span.Text));
}
