using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using Spectre.Console;

namespace EtcdTerminal.App.Screens;

public sealed class SettingsScreen(IAppSettingsRepository _repository)
{
	private const int MinPageSize = 1;
	private const int MaxPageSize = 500;

	private const string SettingsTitle = "Settings";
	private const string PageSizeItem = "PageSize";
	private const string TrimInputValuesItem = "TrimInputValues";
	private const string PageSizeLabel = "Keys per page";
	private const string TrimInputValuesLabel = "Trim input values";
	private const string EnterPageSize = "Enter keys per page (1-500):";
	private const string InvalidPageSize = "[red]Invalid page size. Must be a number from 1 to 500.[/]";
	private const string SettingsSaved = "[green]Settings saved![/]";

	public void Show()
	{
		while (true)
		{
			AnsiConsole.Clear();
			Header.Render();

			var choice = Menu.Show(SettingsTitle, [PageSizeItem, TrimInputValuesItem], FormatItem);

			if (choice is null)
				return;

			if (choice == PageSizeItem)
				EditPageSize();
			else
				ToggleTrimInputValues();
		}
	}

	private static string FormatItem(string item) => item switch
	{
		PageSizeItem => $"{PageSizeLabel} ({AppSettings.PageSize})",
		_ => $"{TrimInputValuesLabel} ({OnOff(AppSettings.TrimInputValues)})"
	};

	private static string OnOff(bool value) => value ? "On" : "Off";

	private void EditPageSize()
	{
		var input = Prompt.Ask(EnterPageSize);

		if (input is null)
			return;

		if (int.TryParse(input, out var pageSize) && pageSize is >= MinPageSize and <= MaxPageSize)
		{
			AppSettings.PageSize = pageSize;
			_repository.Save();
			AnsiConsole.MarkupLine(SettingsSaved);
		}
		else
		{
			AnsiConsole.MarkupLine(InvalidPageSize);
		}

		AnsiConsole.WriteLine();
		PressAnyKeyPrompt.Show();
	}

	private void ToggleTrimInputValues()
	{
		AppSettings.TrimInputValues = !AppSettings.TrimInputValues;
		_repository.Save();
	}
}