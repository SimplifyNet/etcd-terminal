using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Components;

/// <summary>
/// Writes an outcome below whatever the screen just did instead of opening a
/// screen of its own, so the values the user typed stay on screen next to the
/// result. The message, its spacing and the hint that a key continues are
/// blocks on the same canvas, so nothing here moves the footer.
/// </summary>
public sealed class Message(Screen _screen, IKeyReader _keys, ILocalization _localization)
{
	public void ShowSuccess(string text) => Show(text, TextRole.Success);

	public void ShowError(string text) => Show(text, TextRole.Danger);

	public void ShowWarning(string text) => Show(text, TextRole.Warning);

	public void ShowResult(bool ok, string success, string failure)
	{
		if (ok)
			ShowSuccess(success);
		else
			ShowError(failure);
	}

	private void Show(string text, TextRole role)
	{
		var lines = text.Split('\n');
		List<IReadOnlyList<StyledText>> content = [];

		foreach (var line in lines)
			content.Add([new StyledText(line.TrimEnd('\r'), role)]);

		_screen.Write(
		[
			TextBlock.Blank(),
			new TextBlock(content),
			TextBlock.Blank(),
			TextBlock.Line(new StyledText(_localization.PressAnyKey, TextRole.Muted))
		]);

		_keys.ReadKey();
	}
}
