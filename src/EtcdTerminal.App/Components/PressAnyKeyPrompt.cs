using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Components;

/// <summary>
/// Presents content as one complete screen: the banner and the body the
/// caller composed are pinned, the hint that a key continues sits above the
/// footer, and the body scrolls between them when it does not fit. A scroll
/// key — which is what a mouse wheel becomes in alternate scroll mode — moves
/// the body; only any other key continues the screen.
/// </summary>
public sealed class PressAnyKeyPrompt(Screen _screen, IKeyReader _keys, ILocalization _localization)
{
	public void Show(IReadOnlyList<Block> body)
	{
		_screen.OpenPage(body,
		[
			TextBlock.Blank(),
			TextBlock.Line(new StyledText(_localization.PressAnyKey, TextRole.Muted))
		]);

		while (true)
		{
			var step = ScrollKeys.Step(_keys.ReadKey());

			if (step is null)
				return;

			_screen.Scroll(step.Value);
		}
	}
}
