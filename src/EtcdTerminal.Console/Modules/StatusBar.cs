using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.Console.Modules;

public static class StatusBar
{
	public static void Render(EtcdConnectionConfig config)
	{
		var dot = "[green]●[/]";
		var connStr = config.ConnectionString.Length > 50
			? config.ConnectionString[..50] + "..."
			: config.ConnectionString;
		var auth = config.IsAuthenticationEnabled
			? $" [grey]│[/] [yellow]{config.Username}[/]"
			: "";

		AnsiConsole.WriteLine();
		AnsiConsole.Write(new Rule($"{dot} [bold cyan]{config.Name}[/] [grey]│[/] [grey]{connStr}[/]{auth}")
		{
			Style = Style.Parse("grey37"),
			Border = BoxBorder.Ascii
		});
		AnsiConsole.WriteLine();
	}
}
