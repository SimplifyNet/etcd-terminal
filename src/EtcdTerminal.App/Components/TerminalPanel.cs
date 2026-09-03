using EtcdTerminal.Theming;
using Spectre.Console;

namespace EtcdTerminal.App.Components;

public static class TerminalPanel
{
	public static string Bg => $"\x1b[48;2;{ThemeStore.Current.PanelBackground.R};{ThemeStore.Current.PanelBackground.G};{ThemeStore.Current.PanelBackground.B}m";
	public static string DarkBg => $"\x1b[48;2;{ThemeStore.Current.PanelDarkerBackground.R};{ThemeStore.Current.PanelDarkerBackground.G};{ThemeStore.Current.PanelDarkerBackground.B}m";
	public static string White => $"\x1b[38;2;{ThemeStore.Current.White.R};{ThemeStore.Current.White.G};{ThemeStore.Current.White.B}m";
	public static string Grey => $"\x1b[38;2;{ThemeStore.Current.Grey.R};{ThemeStore.Current.Grey.G};{ThemeStore.Current.Grey.B}m";
	public static string Green => $"\x1b[38;2;{ThemeStore.Current.Green.R};{ThemeStore.Current.Green.G};{ThemeStore.Current.Green.B}m";
	public static string Teal => $"\x1b[38;2;{ThemeStore.Current.Teal.R};{ThemeStore.Current.Teal.G};{ThemeStore.Current.Teal.B}m";
	public static string Yellow => $"\x1b[38;2;{ThemeStore.Current.Yellow.R};{ThemeStore.Current.Yellow.G};{ThemeStore.Current.Yellow.B}m";
	public static string Dim => $"\x1b[38;2;{ThemeStore.Current.Dim.R};{ThemeStore.Current.Dim.G};{ThemeStore.Current.Dim.B}m";
	public const string Reset = "\x1b[0m";

	public static Color AccentColor => new(ThemeStore.Current.Accent.R, ThemeStore.Current.Accent.G, ThemeStore.Current.Accent.B);

	public static string Accent => $"\x1b[38;2;{ThemeStore.Current.Accent.R};{ThemeStore.Current.Accent.G};{ThemeStore.Current.Accent.B}m";

	public const string SelectionPointer = "  ❯ ";

	public const string SelectionPointerEmpty = "    ";

	public static void SetDarkBackground() => Console.Write($"\x1b]11;#{ThemeStore.Current.WindowBackground.R:X2}{ThemeStore.Current.WindowBackground.G:X2}{ThemeStore.Current.WindowBackground.B:X2}\x07");

	public static void ResetBackground() => Console.Write("\x1b]111\x07");

	public static void ClearScreen() => Console.Write("\x1b[2J\x1b[H");

	public static string FillRow(string bg) => bg + new string(' ', Console.WindowWidth) + Reset;

	public static void WriteFillRow(string bg) => Console.WriteLine(FillRow(bg));

	public static void WriteRow(string bg, string content) =>
		Console.WriteLine(bg + content + new string(' ', Math.Max(0, Console.WindowWidth - GetVisibleLength(content))) + Reset);

	public static void WriteBorderedFillRow(string bg) =>
		Console.WriteLine(Accent + "│" + Reset + bg + new string(' ', Console.WindowWidth - 1) + Reset);

	public static void WriteBorderedRow(string bg, string content) =>
		Console.WriteLine(Accent + "│" + Reset + bg + content + new string(' ', Math.Max(0, Console.WindowWidth - 1 - GetVisibleLength(content))) + Reset);

	public static void PadCurrentRow(string bg)
	{
		var remaining = Console.WindowWidth - Console.CursorLeft;

		if (remaining > 0)
			Console.Write(bg + new string(' ', remaining) + Reset);
	}

	public static int GetVisibleLength(string s)
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