using EtcdTerminal.Presentation.Theming;
using EtcdTerminal.Presentation;
using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// The single place where a semantic role and the theme palette become a
/// Spectre style. Renderers must not resolve colors on their own.
/// </summary>
public sealed class RoleStyleMapper(ITheme _theme)
{
	public Style Resolve(TextRole role) => role switch
	{
		TextRole.Primary => Foreground(_theme.Primary),
		TextRole.Secondary => Foreground(_theme.Secondary),
		TextRole.Success => Foreground(_theme.Success),
		TextRole.Danger => Foreground(_theme.Danger),
		TextRole.Warning => Foreground(_theme.Warning),
		TextRole.Muted => Foreground(_theme.Muted),
		TextRole.Subtle => Foreground(_theme.Subtle),
		TextRole.Accent => Foreground(_theme.Accent),
		_ => Style.Plain
	};

	/// <summary>
	/// Renders literal spans as one paragraph. Text is passed to Spectre as
	/// plain content, never as markup. A line with no spans is a blank line and
	/// still has to occupy one row: Spectre drops a paragraph that was never
	/// given any text.
	/// </summary>
	public Paragraph Build(IReadOnlyList<StyledText> spans)
	{
		var paragraph = new Paragraph();

		foreach (var span in spans)
			paragraph.Append(span.Text, Resolve(span.Role));

		if (spans.Count == 0)
			paragraph.Append(string.Empty, Style.Plain);

		return paragraph;
	}

	private static Style Foreground(RgbColor color) => new(foreground: new Color(color.R, color.G, color.B));
}
