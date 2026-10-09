using EtcdTerminal.App.Components;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Keys;

/// <summary>
/// The prompts of an edit or a delete: what the screen opens above the value
/// the user is about to change or remove.
/// </summary>
public sealed class KeyEditPrompt(Screen _screen, KeyBrowseLayout _layout, UserInput _input, ILocalizationCatalog _localizations)
{
	public string? AskNewValue(EtcdKeyValue key)
	{
		_screen.Open(
		[
			_layout.Detail(_localizations.Current.EditingKey, key.Key, TextRole.Primary),
			_layout.Detail(_localizations.Current.CurrentValue, DisplayText.Sanitize(key.Value), TextRole.Success)
		]);

		return _input.Ask(_localizations.Current.EnterNewValue, key.Value);
	}

	public void ShowDeleting(EtcdKeyValue key) =>
		_screen.Open([_layout.Detail(_localizations.Current.DeleteKey, key.Key, TextRole.Danger)]);
}
