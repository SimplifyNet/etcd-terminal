using EtcdTerminal.Models;
using Simplify.System;

namespace EtcdTerminal.App.Components;

public static class StatusBar
{
	public static void Render(EtcdConnectionConfig? config = null)
	{
		var left = $"{TerminalPanel.Grey}  {TerminalPanel.White}\u2191/\u2193{TerminalPanel.Grey} navigate \u00b7 {TerminalPanel.White}Enter{TerminalPanel.Grey} confirm/select \u00b7 {TerminalPanel.White}Esc{TerminalPanel.Grey} back  ";
		var version = GetVersion();

		var rightPadding = "  ";

		string right;
		if (config is not null)
		{
			var connStr = config.ConnectionString.Length > 50
				? config.ConnectionString[..50] + "..."
				: config.ConnectionString;
			right = $"{TerminalPanel.Green}\u2022{TerminalPanel.Teal} {config.Name} {TerminalPanel.Dim}\u00b7{TerminalPanel.Grey} {connStr}";
			if (config.IsAuthenticationEnabled)
				right += $" {TerminalPanel.Dim}\u00b7{TerminalPanel.Yellow} {config.Username}";
			right += $" {TerminalPanel.Grey}v{TerminalPanel.White}{version}";
		}
		else
			right = $"{TerminalPanel.Grey}v{TerminalPanel.White}{version}";

		var visibleWidth = TerminalPanel.GetVisibleLength(left) + TerminalPanel.GetVisibleLength(right) + rightPadding.Length;
		var pad = Console.WindowWidth - visibleWidth;
		if (pad < 0) pad = 0;
		var content = TerminalPanel.Bg + TerminalPanel.Grey + left + new string(' ', pad) + right + TerminalPanel.Grey + rightPadding + TerminalPanel.Reset;

		Console.CursorTop = Console.WindowHeight - 3;
		Console.CursorLeft = 0;
		Console.Write(TerminalPanel.FillRow(TerminalPanel.Bg));

		Console.CursorTop = Console.WindowHeight - 2;
		Console.CursorLeft = 0;
		Console.Write(content);

		Console.CursorTop = Console.WindowHeight - 1;
		Console.CursorLeft = 0;
		Console.Write(TerminalPanel.FillRow(TerminalPanel.Bg));
	}

	private static string GetVersion()
	{
		var version = AssemblyInfo.Entry.Version;

		return $"{version.Major}.{version.Minor}" + (version.Build != 0 ? "." + version.Build : "");
	}
}
