using EtcdTerminal.Terminal;
using Spectre.Console;

namespace EtcdTerminal.App.Components;

public sealed class PressAnyKeyPrompt(ITerminal _terminal)
{
	private const string PressAnyKeyMarkup = "[grey]Press any key to continue...[/]";

	public void Show()
	{
		AnsiConsole.MarkupLine(PressAnyKeyMarkup);
		_terminal.ReadKey();
	}
}
