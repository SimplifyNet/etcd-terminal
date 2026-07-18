using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.Console.Helpers;

public static class StatusBar
{
	public static void Render(EtcdConnectionConfig config)
	{
		var ssl = config.UseSsl ? "[green]SSL[/]" : "[grey]no SSL[/]";
		var auth = config.IsAuthenticationEnabled ? "[yellow]auth[/]" : "[grey]no auth[/]";

		AnsiConsole.WriteLine();
		AnsiConsole.Write(new Rule($"[bold cyan]{config.Name}[/]  [grey]│[/]  [grey]{config.ConnectionString}[/]  [grey]│[/]  {ssl}  [grey]│[/]  {auth}")
		{
			Style = Style.Parse("grey37"),
			Border = BoxBorder.Ascii
		});
		AnsiConsole.WriteLine();
	}
}
