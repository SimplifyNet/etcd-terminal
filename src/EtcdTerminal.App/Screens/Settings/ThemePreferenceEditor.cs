using EtcdTerminal.App.Engine;
using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Screens.Settings;

/// <summary>
/// The theme setting. The highlighted theme is previewed live while the menu
/// is open; cancelling or a failed save brings the previous theme back.
/// </summary>
public sealed class ThemePreferenceEditor(Menu _menu, SettingsWriter _writer, IThemeCatalog _themes, ILocalizationCatalog _languages) : ISettingsEntry
{
	public SettingsAction Action => SettingsAction.SelectTheme;

	public string Label => $"{_languages.Current.ThemeLabel} ({Name(_themes.Current)})";

	/// <summary>
	/// The first-run prompt: shown only while no theme was ever saved. A
	/// dismissed prompt keeps the default and asks again on the next start.
	/// </summary>
	public void ShowIfMissing()
	{
		if (_writer.Current.ThemeId is null)
			Edit();
	}

	public bool Edit()
	{
		var previous = _themes.Current.Id;
		var choices = _themes.Themes.Select(theme => new Choice<string>(theme.Id, Name(theme))).ToArray();
		var selected = _menu.Show(_languages.Current.SelectThemePrompt, choices, onHighlight: choice => _themes.Set(choice.Id), selectedId: previous);

		if (selected is null || !_writer.SaveTheme(selected.Id))
			_themes.Set(previous);

		return false;
	}

	private static string Name(ITheme theme) => $"{theme.Name} ({theme.Variant})";
}
