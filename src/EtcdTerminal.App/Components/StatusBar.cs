using EtcdTerminal.Models;
using Simplify.System;

namespace EtcdTerminal.App.Components;

public static class StatusBar
{
	public static void Render(EtcdConnectionConfig? config = null)
	{
		var bgSeq = "\x1b[48;2;27;28;30m";
		var fgSeq = "\x1b[38;2;210;210;210m";
		var greenSeq = "\x1b[38;2;0;200;0m";
		var resetSeq = "\x1b[0m";
		var fill = new string(' ', Console.WindowWidth);

		var left = "  (\u2191/\u2193 navigate, Enter confirm, Esc back)  ";
		var version = GetVersion();

		string content;
		if (config is not null)
		{
			var connStr = config.ConnectionString.Length > 50
				? config.ConnectionString[..50] + "..."
				: config.ConnectionString;
			var auth = config.IsAuthenticationEnabled
				? $" \x1b[38;2;128;128;128m│\x1b[38;2;210;210;210m {config.Username}"
				: "";
			var right = $"{greenSeq}\u25cf{fgSeq} {config.Name} \x1b[38;2;128;128;128m│\x1b[38;2;210;210;210m {connStr}{auth}  v{version}  ";
			var pad = Console.WindowWidth - left.Length - GetVisibleLength(right);
			if (pad < 1) pad = 1;
			content = bgSeq + fgSeq + left + new string(' ', pad) + right + resetSeq;
		}
		else
		{
			var right = $"v{version}  ";
			var pad = Console.WindowWidth - left.Length - right.Length;
			if (pad < 1) pad = 1;
			content = bgSeq + fgSeq + left + new string(' ', pad) + right + resetSeq;
		}

		Console.CursorTop = Console.WindowHeight - 3;
		Console.CursorLeft = 0;
		Console.Write(bgSeq + fill + resetSeq);

		Console.CursorTop = Console.WindowHeight - 2;
		Console.CursorLeft = 0;
		Console.Write(content);

		Console.CursorTop = Console.WindowHeight - 1;
		Console.CursorLeft = 0;
		Console.Write(bgSeq + fill + resetSeq);
	}

	private static string GetVersion()
	{
		var version = AssemblyInfo.Entry.Version;

		return $"{version.Major}.{version.Minor}" + (version.Build != 0 ? "." + version.Build : "");
	}

	private static int GetVisibleLength(string s)
	{
		var len = 0;
		for (var i = 0; i < s.Length; i++)
		{
			if (s[i] == '\x1b')
				while (i < s.Length && s[i] != 'm') i++;
			else
				len++;
		}
		return len;
	}
}
