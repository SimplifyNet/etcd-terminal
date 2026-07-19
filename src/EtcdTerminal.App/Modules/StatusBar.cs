using EtcdTerminal;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Modules;

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
		var bg = new Style(background: Color.FromHex("1b1c1e"));

		AnsiConsole.Write(new Rule(" ") { Style = bg, Border = BoxBorder.None });
		AnsiConsole.Write(new Rule($"     {dot} [bold cyan]{config.Name}[/] [grey]│[/] [grey]{connStr}[/]{auth}     ")
		{
			Style = bg,
			Border = BoxBorder.None
		});
		AnsiConsole.Write(new Rule(" ") { Style = bg, Border = BoxBorder.None });
		AnsiConsole.WriteLine();
	}
}
