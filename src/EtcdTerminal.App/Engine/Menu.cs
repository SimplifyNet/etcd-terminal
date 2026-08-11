using EtcdTerminal.App.Components;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Engine;

public static class Menu
{
	private static readonly Style _highlightStyle = new(foreground: TerminalPanel.AccentColor);

	public static string? Show(string title, IEnumerable<string> choices, Func<string, string>? displayConverter = null, EtcdConnectionConfig? config = null)
	{
		var prompt = new SelectionPrompt<string>()
			.AddCancelResult(() => null!)
			.HighlightStyle(_highlightStyle)
			.PageSize(10)
			.WrapAround();

		if (!string.IsNullOrEmpty(title))
			prompt.Title(title);

		prompt.AddChoices(choices);

		if (displayConverter is not null)
			prompt.UseConverter(displayConverter);

		var savedTop = Console.CursorTop;

		StatusBar.Render(config);
		Console.CursorTop = savedTop;
		Console.CursorLeft = 0;

		return AnsiConsole.Prompt(prompt);
	}
}
