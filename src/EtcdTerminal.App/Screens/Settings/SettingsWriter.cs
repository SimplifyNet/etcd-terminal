using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Settings;

public sealed class SettingsWriter(IAppSettingsStore _settings, Message _message, ILocalizationCatalog _localizations)
{
	public AppSettings Current => _settings.Current;

	public bool TrySave(AppSettings updated)
	{
		try
		{
			_settings.Save(updated);

			return true;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			_message.ShowError(_localizations.Current.FailedSaveSettings);

			return false;
		}
	}

	public bool SaveLanguage(string languageCode) => TrySave(_settings.Current with { LanguageCode = languageCode });

	public bool SaveTheme(string themeId) => TrySave(_settings.Current with { ThemeId = themeId });
}
