using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Settings;

public sealed class SettingsScreen(Menu _menu, Prompt _prompt, Message _message, ILocalization _localization, IAppSettingsStore _settings)
{
	private const int MinPageSize = 1;
	private const int MaxPageSize = 500;

	public void Show()
	{
		while (true)
		{
			SettingsAction? action = _menu.Show<SettingsAction>(_localization.SettingsTitle,
			[
				new(SettingsAction.EditPageSize, $"{_localization.PageSizeLabel} ({_settings.Current.PageSize})"),
				new(SettingsAction.ToggleTrimInputValues, $"{_localization.TrimInputValuesLabel} ({OnOff(_settings.Current.TrimInputValues)})")
			])?.Id;

			if (action is null)
				return;

			switch (action)
			{
				case SettingsAction.EditPageSize:
					EditPageSize();
					break;
				case SettingsAction.ToggleTrimInputValues:
					ToggleTrimInputValues();
					break;
			}
		}
	}

	private string OnOff(bool value) => value ? _localization.On : _localization.Off;

	private void EditPageSize()
	{
		var input = _prompt.Ask(_localization.EnterPageSize, trim: _settings.Current.TrimInputValues);

		if (input is null)
			return;

		if (!int.TryParse(input, out var pageSize) || pageSize is not (>= MinPageSize and <= MaxPageSize))
		{
			_message.ShowError(_localization.InvalidPageSize);

			return;
		}

		var updated = new AppSettings
		{
			PageSize = pageSize,
			TrimInputValues = _settings.Current.TrimInputValues
		};

		if (TrySave(updated))
			_message.ShowSuccess(_localization.SettingsSaved);
	}

	private void ToggleTrimInputValues()
	{
		var updated = new AppSettings
		{
			PageSize = _settings.Current.PageSize,
			TrimInputValues = !_settings.Current.TrimInputValues
		};

		TrySave(updated);
	}

	private bool TrySave(AppSettings updated)
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
}
