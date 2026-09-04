using EtcdTerminal.Terminal;
using EtcdTerminal.Theming;
using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

public sealed class ConsoleTerminal : ITerminal
{
	public string Bg => $"\x1b[48;2;{ThemeStore.Current.PanelBackground.R};{ThemeStore.Current.PanelBackground.G};{ThemeStore.Current.PanelBackground.B}m";
	public string DarkBg => $"\x1b[48;2;{ThemeStore.Current.PanelDarkerBackground.R};{ThemeStore.Current.PanelDarkerBackground.G};{ThemeStore.Current.PanelDarkerBackground.B}m";
	public string White => $"\x1b[38;2;{ThemeStore.Current.White.R};{ThemeStore.Current.White.G};{ThemeStore.Current.White.B}m";
	public string Grey => $"\x1b[38;2;{ThemeStore.Current.Grey.R};{ThemeStore.Current.Grey.G};{ThemeStore.Current.Grey.B}m";
	public string Green => $"\x1b[38;2;{ThemeStore.Current.Green.R};{ThemeStore.Current.Green.G};{ThemeStore.Current.Green.B}m";
	public string Red => $"\x1b[38;2;{ThemeStore.Current.Red.R};{ThemeStore.Current.Red.G};{ThemeStore.Current.Red.B}m";
	public string Teal => $"\x1b[38;2;{ThemeStore.Current.Teal.R};{ThemeStore.Current.Teal.G};{ThemeStore.Current.Teal.B}m";
	public string Yellow => $"\x1b[38;2;{ThemeStore.Current.Yellow.R};{ThemeStore.Current.Yellow.G};{ThemeStore.Current.Yellow.B}m";
	public string Dim => $"\x1b[38;2;{ThemeStore.Current.Dim.R};{ThemeStore.Current.Dim.G};{ThemeStore.Current.Dim.B}m";
	public string Reset => "\x1b[0m";
	public string Accent => $"\x1b[38;2;{ThemeStore.Current.Accent.R};{ThemeStore.Current.Accent.G};{ThemeStore.Current.Accent.B}m";
	public string SelectionPointer => "  ❯ ";
	public string SelectionPointerEmpty => "    ";

	public int WindowWidth => Console.WindowWidth;

	public int WindowHeight => Console.WindowHeight;

	public int CursorLeft => Console.CursorLeft;

	public int CursorTop => Console.CursorTop;

	public void Write(string text) => Console.Write(text);

	public void Write(string text, TerminalColor color) =>
		Console.Write(GetColorEscape(color) + text + Reset);

	public void WriteLine(string text) => Console.WriteLine(text);

	public void WriteLine(string text, TerminalColor color) =>
		Console.WriteLine(GetColorEscape(color) + text + Reset);

	public void WriteLine() => Console.WriteLine();

	public void Clear() => Console.Write("\x1b[2J\x1b[3J\x1b[H");

	public void SetCursorPosition(int left, int top) => Console.SetCursorPosition(left, top);

	public void SetBackground(string ansiColor) => Console.Write(ansiColor);

	public void ResetBackground() => Console.Write("\x1b]111\x07");

	public void ResetColor() => Console.ResetColor();

	public void SetDarkBackground() => Write($"\x1b]11;#{ThemeStore.Current.WindowBackground.R:X2}{ThemeStore.Current.WindowBackground.G:X2}{ThemeStore.Current.WindowBackground.B:X2}\x07");

	public void ClearScreen() => Clear();

	public string FillRow(string bg) => bg + new string(' ', WindowWidth) + Reset;

	public void WriteFillRow(string bg) => WriteLine(FillRow(bg));

	public void WriteRow(string bg, string content) =>
		WriteLine(bg + content + new string(' ', Math.Max(0, WindowWidth - GetVisibleLength(content))) + Reset);

	public void WriteBorderedFillRow(string bg) =>
		WriteLine(Accent + "│" + Reset + bg + new string(' ', WindowWidth - 1) + Reset);

	public void WriteBorderedRow(string bg, string content) =>
		WriteLine(Accent + "│" + Reset + bg + content + new string(' ', Math.Max(0, WindowWidth - 1 - GetVisibleLength(content))) + Reset);

	public void PadCurrentRow(string bg)
	{
		var remaining = WindowWidth - CursorLeft;

		if (remaining > 0)
			Write(bg + new string(' ', remaining) + Reset);
	}

	public int GetVisibleLength(string s)
	{
		var len = 0;

		for (var i = 0; i < s.Length; i++)
			if (s[i] == '\x1b')
				while (i < s.Length && s[i] != 'm') i++;
			else
				len++;

		return len;
	}

	public void Initialize()
	{
		Console.OutputEncoding = System.Text.Encoding.UTF8;

		SetDarkBackground();
		SetCursorVisible(false);
	}

	public ConsoleKeyInfo ReadKey() => Console.ReadKey(true);

	public void Flush()
	{
		ResetColor();
		Console.Out.Flush();
	}

	public void WriteException(Exception ex)
	{
		AnsiConsole.WriteException(ex);
		SetCursorVisible(false);
	}

	public void SetCursorVisible(bool visible) => Console.Write(visible ? "\x1b[?25h" : "\x1b[?25l");

	public async Task ShowStatusAsync(string message, Func<CancellationToken, Task> action)
	{
		try
		{
			await AnsiConsole.Status()
				.StartAsync(message, async _ => await action(CancellationToken.None));
		}
		finally
		{
			SetCursorVisible(false);
		}
	}

	private string GetColorEscape(TerminalColor color) => color switch
	{
		TerminalColor.Success => Green,
		TerminalColor.Error => Red,
		TerminalColor.Warning => Yellow,
		TerminalColor.Muted => Grey,
		_ => White
	};
}
