using Spectre.Console;

namespace EtcdTerminal.App.Components;

public static class PressAnyKeyPrompt
{
	private const string PressAnyKeyMarkup = "[grey]Press any key to continue...[/]";

	public static void Show()
	{
		AnsiConsole.MarkupLine(PressAnyKeyMarkup);
		Console.ReadKey(true);
	}
}
