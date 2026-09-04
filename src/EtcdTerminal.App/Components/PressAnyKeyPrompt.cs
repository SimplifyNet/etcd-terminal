using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;
using Spectre.Console;

namespace EtcdTerminal.App.Components;

public sealed class PressAnyKeyPrompt(ITerminal _terminal)
{
	public void Show()
	{
		AnsiConsole.MarkupLine($"[grey]{LocalizationStore.Current.PressAnyKey}[/]");
		_terminal.ReadKey();
	}
}
