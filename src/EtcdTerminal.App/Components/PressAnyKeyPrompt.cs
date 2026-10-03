using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Components;

/// <summary>
/// Presents content as one complete screen: the banner, the body the caller
/// composed, the hint that a key continues, and the session footer. The screen
/// stays on while waiting, so the hint sits above the footer and a single
/// component owns every row of the viewport.
/// </summary>
public sealed class PressAnyKeyPrompt(Screen _screen, IKeyReader _keys, ILocalization _localization)
{
	public void Show(IReadOnlyList<Block> body)
	{
		_screen.Open(
		[
			.. body,
			TextBlock.Blank(),
			TextBlock.Line(new StyledText(_localization.PressAnyKey, TextRole.Muted))
		]);

		_keys.ReadKey();
	}
}
