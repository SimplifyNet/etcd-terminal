namespace EtcdTerminal.App.Components;

public static class TerminalPanel
{
	public const string Bg = "\x1b[48;2;27;28;30m";
	public const string DarkBg = "\x1b[48;2;21;22;24m";
	public const string White = "\x1b[38;2;255;255;255m";
	public const string Grey = "\x1b[38;2;128;128;128m";
	public const string Accent = "\x1b[38;2;220;95;51m";
	public const string Green = "\x1b[38;2;0;200;0m";
	public const string Teal = "\x1b[38;2;0;180;180m";
	public const string Yellow = "\x1b[38;2;255;200;0m";
	public const string Dim = "\x1b[38;2;80;80;80m";
	public const string Reset = "\x1b[0m";

	public static void SetDarkBackground() => Console.Write("\x1b]11;#0a0a0a\x07");

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
