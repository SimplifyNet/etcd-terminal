using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using Spectre.Console;

namespace EtcdTerminal.App.Screens;

public sealed class SettingsScreen(IAppSettingsRepository _repository, Menu _menu)
{
	private const int MinPageSize = 1;
	private const int MaxPageSize = 500;

	public void Show()
	{
		while (true)
		{
			AnsiConsole.Clear();
			Header.Render();

			var choice = _menu.Show(LocalizationStore.Current.SettingsTitle, [LocalizationStore.Current.PageSizeItem, LocalizationStore.Current.TrimInputValuesItem], FormatItem);

			if (choice is null)
				return;

			if (choice == LocalizationStore.Current.PageSizeItem)
				EditPageSize();
			else
				ToggleTrimInputValues();
		}
	}

	private static string FormatItem(string item) => item switch
	{
		var _ when item == LocalizationStore.Current.PageSizeItem => $"{LocalizationStore.Current.PageSizeLabel} ({AppSettingsStore.Current.PageSize})",
		_ => $"{LocalizationStore.Current.TrimInputValuesLabel} ({OnOff(AppSettingsStore.Current.TrimInputValues)})"
	};

	private static string OnOff(bool value) => value ? LocalizationStore.Current.On : LocalizationStore.Current.Off;

	private void EditPageSize()
	{
		var input = Prompt.Ask(LocalizationStore.Current.EnterPageSize);

		if (input is null)
			return;

		if (int.TryParse(input, out var pageSize) && pageSize is >= MinPageSize and <= MaxPageSize)
		{
			AppSettingsStore.Current.PageSize = pageSize;
			_repository.Save(AppSettingsStore.Current);
			AnsiConsole.MarkupLine(LocalizationStore.Current.SettingsSaved);
		}
		else
		{
			AnsiConsole.MarkupLine(LocalizationStore.Current.InvalidPageSize);
		}

		AnsiConsole.WriteLine();
		PressAnyKeyPrompt.Show();
	}

	private void ToggleTrimInputValues()
	{
		AppSettingsStore.Current.TrimInputValues = !AppSettingsStore.Current.TrimInputValues;
		_repository.Save(AppSettingsStore.Current);
	}
}
