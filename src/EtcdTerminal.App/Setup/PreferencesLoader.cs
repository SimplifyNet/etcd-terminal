using EtcdTerminal.Configuration;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Setup;

/// <summary>
/// Reads the settings file and puts the saved language and theme into the
/// catalogs. A missing or unknown value leaves the catalog default in place.
/// </summary>
public sealed class PreferencesLoader(IAppSettingsStore _settings, ILocalizationCatalog _localizations, IThemeCatalog _themes)
{
	public void Load()
	{
		_settings.Reload();
		_localizations.Set(_settings.Current.LanguageCode);
		_themes.Set(_settings.Current.ThemeId);
	}
}
