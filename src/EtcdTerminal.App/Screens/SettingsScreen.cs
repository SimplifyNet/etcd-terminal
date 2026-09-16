using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Screens;

public sealed class SettingsScreen(ITerminal _terminal, IAppSettingsRepository _repository, Menu _menu, Prompt _prompt, Message _message)
{
	private const int MinPageSize = 1;
	private const int MaxPageSize = 500;

	public void Show()
	{
		while (true)
		{
			_terminal.Clear();
			Header.Render(_terminal);

			SettingsAction? action = _menu.Show<SettingsAction>(LocalizationStore.Current.SettingsTitle,
			[
				new(SettingsAction.EditPageSize, $"{LocalizationStore.Current.PageSizeLabel} ({AppSettingsStore.Current.PageSize})"),
				new(SettingsAction.ToggleTrimInputValues, $"{LocalizationStore.Current.TrimInputValuesLabel} ({OnOff(AppSettingsStore.Current.TrimInputValues)})")
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

	private static string OnOff(bool value) => value ? LocalizationStore.Current.On : LocalizationStore.Current.Off;

	private void EditPageSize()
	{
		var input = _prompt.Ask(LocalizationStore.Current.EnterPageSize);

		if (input is null)
			return;

		if (int.TryParse(input, out var pageSize) && pageSize is >= MinPageSize and <= MaxPageSize)
		{
			var updated = new AppSettings
			{
				PageSize = pageSize,
				TrimInputValues = AppSettingsStore.Current.TrimInputValues
			};

			try
			{
				_repository.Save(updated);

				AppSettingsStore.Current = updated;

				_message.ShowSuccess(LocalizationStore.Current.SettingsSaved);
			}
			catch
			{
				_message.ShowError(LocalizationStore.Current.FailedSaveSettings);
			}
		}
		else
			_message.ShowError(LocalizationStore.Current.InvalidPageSize);
	}

	private void ToggleTrimInputValues()
	{
		var updated = new AppSettings
		{
			PageSize = AppSettingsStore.Current.PageSize,
			TrimInputValues = !AppSettingsStore.Current.TrimInputValues
		};

		try
		{
			_repository.Save(updated);

			AppSettingsStore.Current = updated;
		}
		catch
		{
			_message.ShowError(LocalizationStore.Current.FailedSaveSettings);
		}
	}
}
