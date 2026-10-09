using EtcdTerminal.App.Engine;
using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Settings;

/// <summary>
/// The language setting. The catalog is switched only after the choice is
/// saved, so a failed save leaves the running language untouched.
/// </summary>
public sealed class LanguagePreferenceEditor(Menu _menu, SettingsWriter _writer, ILocalizationCatalog _languages) : ISettingsEntry
{
	public SettingsAction Action => SettingsAction.SelectLanguage;

	public string Label => $"{_languages.Current.LanguageLabel} ({_languages.Current.Name})";

	/// <summary>
	/// The first-run prompt: shown only while no language was ever saved. A
	/// dismissed prompt keeps the default and asks again on the next start.
	/// </summary>
	public void ShowIfMissing()
	{
		if (_writer.Current.LanguageCode is null)
			Edit();
	}

	/// <summary>
	/// Returns whether the running language changed; the caller rebuilds the
	/// screens so every component picks up the new text.
	/// </summary>
	public bool Edit()
	{
		var choices = _languages.Localizations.Select(language => new Choice<string>(language.LanguageCode, language.Name)).ToArray();
		var selected = _menu.Show(_languages.Current.SelectLanguagePrompt, choices, selectedId: _languages.Current.LanguageCode);

		if (selected is null || !_writer.SaveLanguage(selected.Id))
			return false;

		return _languages.Set(selected.Id);
	}
}
