using EtcdTerminal.Terminal;
using EtcdTerminal.Theming;
using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

public sealed class ConsoleTerminal : ITerminal
{
	private readonly Dictionary<string, string> _escapeCache = new();
	private ITheme? _cachedTheme;

	public string Bg => CachedEscape(nameof(Bg), ThemeStore.Current.PanelBackground, BgEscape);
	public string DarkBg => CachedEscape(nameof(DarkBg), ThemeStore.Current.PanelDarkerBackground, BgEscape);
	public string White => CachedEscape(nameof(White), ThemeStore.Current.White, FgEscape);
	public string Grey => CachedEscape(nameof(Grey), ThemeStore.Current.Grey, FgEscape);
	public string Green => CachedEscape(nameof(Green), ThemeStore.Current.Green, FgEscape);
	public string Red => CachedEscape(nameof(Red), ThemeStore.Current.Red, FgEscape);
	public string Teal => CachedEscape(nameof(Teal), ThemeStore.Current.Teal, FgEscape);
	public string Yellow => CachedEscape(nameof(Yellow), ThemeStore.Current.Yellow, FgEscape);
	public string Dim => CachedEscape(nameof(Dim), ThemeStore.Current.Dim, FgEscape);
	public string Reset => "\x1b[0m";
	public string Accent => CachedEscape(nameof(Accent), ThemeStore.Current.Accent, FgEscape);
	public string SelectionPointer => "  ❯ ";
	public string Indent => "    ";

	public int WindowWidth => Console.WindowWidth;

	public bool KeyAvailable => Console.KeyAvailable;

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

	public void WriteIndentedLine(string text) => Console.WriteLine(Indent + text);

	public void WriteIndentedLine(string text, TerminalColor color) =>
		Console.WriteLine(GetColorEscape(color) + Indent + text + Reset);

	public void Clear() => Console.Write("\x1b[2J\x1b[3J\x1b[H");

	public void SetCursorPosition(int left, int top) => Console.SetCursorPosition(left, top);

	public void SetBackground(string ansiColor) => Console.Write(ansiColor);

	public void ResetBackground() => Console.Write("\x1b]111\x07");

	public void ResetColor() => Console.ResetColor();

	public void SetDarkBackground() => Write($"\x1b]11;#{ThemeStore.Current.WindowBackground.R:X2}{ThemeStore.Current.WindowBackground.G:X2}{ThemeStore.Current.WindowBackground.B:X2}\x07");

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

	public void WriteTable(TableData table)
	{
		var spectreTable = new Table();

		if (table.Title is not null)
			spectreTable.Title = new TableTitle($"[bold]{Markup.Escape(table.Title)}[/]");

		foreach (var column in table.Columns)
			spectreTable.AddColumn(Markup.Escape(column));

		foreach (var row in table.Rows)
			spectreTable.AddRow([.. row.Select(Markup.Escape)]);

		AnsiConsole.Write(spectreTable);
	}

	public void WriteBanner(string text)
	{
		var banner = ThemeStore.Current.Banner;

		AnsiConsole.Write(new FigletText(text).Color(new Color(banner.R, banner.G, banner.B)).Centered());
	}

	public void ClearLine() => Console.Write("\r\x1b[2K");

	public void ClearToEndOfScreen() => Console.Write("\x1b[J");

	public void OnInterrupt(Action handler) => Console.CancelKeyPress += (_, args) =>
	{
		args.Cancel = true;
		handler();
	};

	private string GetColorEscape(TerminalColor color) => color switch
	{
		TerminalColor.Success => Green,
		TerminalColor.Error => Red,
		TerminalColor.Warning => Yellow,
		TerminalColor.Muted => Grey,
		_ => White
	};

	private string CachedEscape(string key, RgbColor color, Func<RgbColor, string> build)
	{
		if (!ReferenceEquals(ThemeStore.Current, _cachedTheme))
		{
			_cachedTheme = ThemeStore.Current;
			_escapeCache.Clear();
		}

		if (!_escapeCache.TryGetValue(key, out var escape))
		{
			escape = build(color);
			_escapeCache[key] = escape;
		}

		return escape;
	}

	private static string FgEscape(RgbColor color) => $"\x1b[38;2;{color.R};{color.G};{color.B}m";

	private static string BgEscape(RgbColor color) => $"\x1b[48;2;{color.R};{color.G};{color.B}m";
}
