using EtcdTerminal.Models;
using Simplify.System;

namespace EtcdTerminal.App.Components;

public static class StatusBar
{
	public static void Render(EtcdConnectionConfig? config = null)
	{
		var bgSeq = "\x1b[48;2;27;28;30m";
		var fgSeq = "\x1b[38;2;128;128;128m";
		var greenSeq = "\x1b[38;2;0;200;0m";
		var whiteSeq = "\x1b[38;2;255;255;255m";
		var tealSeq = "\x1b[38;2;0;180;180m";
		var yellowSeq = "\x1b[38;2;255;200;0m";
		var dimSeq = "\x1b[38;2;80;80;80m";
		var resetSeq = "\x1b[0m";
		var fill = new string(' ', Console.WindowWidth);

		var left = $"{fgSeq}  {whiteSeq}\u2191/\u2193{fgSeq} navigate \u00b7 {whiteSeq}Enter{fgSeq} confirm/select \u00b7 {whiteSeq}Esc{fgSeq} back  ";
		var version = GetVersion();

		var rightPadding = "  ";

		string content;
		if (config is not null)
		{
			var connStr = config.ConnectionString.Length > 50
				? config.ConnectionString[..50] + "..."
				: config.ConnectionString;
			var right = $"{greenSeq}\u2022{tealSeq} {config.Name} {dimSeq}\u00b7{fgSeq} {connStr}";
			if (config.IsAuthenticationEnabled)
				right += $" {dimSeq}\u00b7{yellowSeq} {config.Username}";
			right += $" {fgSeq}v{whiteSeq}{version}";
			var visibleWidth = GetVisibleLength(left) + GetVisibleLength(right) + rightPadding.Length;
			var pad = Console.WindowWidth - visibleWidth;
			if (pad < 0) pad = 0;
			content = bgSeq + fgSeq + left + new string(' ', pad) + right + fgSeq + rightPadding + resetSeq;
		}
		else
		{
			var right = $"{fgSeq}v{whiteSeq}{version}";
			var visibleWidth = GetVisibleLength(left) + GetVisibleLength(right) + rightPadding.Length;
			var pad = Console.WindowWidth - visibleWidth;
			if (pad < 0) pad = 0;
			content = bgSeq + fgSeq + left + new string(' ', pad) + right + fgSeq + rightPadding + resetSeq;
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
