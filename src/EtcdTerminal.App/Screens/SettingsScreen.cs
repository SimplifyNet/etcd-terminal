using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Screens;

public sealed class SettingsScreen(ScreenLayout _screenLayout, IAppSettingsRepository _repository, Menu _menu, Prompt _prompt, Message _message, ILocalization _localization, IAppSettingsStore _settings)
{
	private const int MinPageSize = 1;
	private const int MaxPageSize = 500;

	public void Show()
	{
		while (true)
		{
			_screenLayout.RenderHeader();

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
		var input = _prompt.Ask(_localization.EnterPageSize);

		if (input is null)
			return;

		if (int.TryParse(input, out var pageSize) && pageSize is >= MinPageSize and <= MaxPageSize)
		{
			var updated = new AppSettings
			{
				PageSize = pageSize,
				TrimInputValues = _settings.Current.TrimInputValues
			};

			try
			{
				_repository.Save(updated);

				_settings.Update(updated);

				_message.ShowSuccess(_localization.SettingsSaved);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				_message.ShowError(_localization.FailedSaveSettings);
			}
		}
		else
			_message.ShowError(_localization.InvalidPageSize);
	}

	private void ToggleTrimInputValues()
	{
		var updated = new AppSettings
		{
			PageSize = _settings.Current.PageSize,
			TrimInputValues = !_settings.Current.TrimInputValues
		};

		try
		{
			_repository.Save(updated);

			_settings.Update(updated);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			_message.ShowError(_localization.FailedSaveSettings);
		}
	}
}
