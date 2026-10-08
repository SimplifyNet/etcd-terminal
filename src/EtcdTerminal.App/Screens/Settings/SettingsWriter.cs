using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Settings;

public sealed class SettingsWriter(IAppSettingsStore _settings, Message _message, ILocalization _localization)
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
			_message.ShowError(_localization.FailedSaveSettings);

			return false;
		}
	}

	public void ToggleTrimInputValues()
	{
		var updated = new AppSettings
		{
			PageSize = _settings.Current.PageSize,
			TrimInputValues = !_settings.Current.TrimInputValues
		};

		TrySave(updated);
	}
}
