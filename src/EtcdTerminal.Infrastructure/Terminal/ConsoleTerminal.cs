using System.Buffers;
using System.Reflection;
using System.Text;
using EtcdTerminal.Terminal;
using EtcdTerminal.Theming;
using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

public sealed class ConsoleTerminal(ITheme _theme) : ITerminal
{
	private readonly Dictionary<string, string> _escapeCache = new();

	public string PanelBackground => CachedEscape(nameof(PanelBackground), _theme.PanelBackground, BgEscape);
	public string PanelDarkerBackground => CachedEscape(nameof(PanelDarkerBackground), _theme.PanelDarkerBackground, BgEscape);
	public string Primary => CachedEscape(nameof(Primary), _theme.Primary, FgEscape);
	public string Secondary => CachedEscape(nameof(Secondary), _theme.Secondary, FgEscape);
	public string Success => CachedEscape(nameof(Success), _theme.Success, FgEscape);
	public string Danger => CachedEscape(nameof(Danger), _theme.Danger, FgEscape);
	public string Warning => CachedEscape(nameof(Warning), _theme.Warning, FgEscape);
	public string Muted => CachedEscape(nameof(Muted), _theme.Muted, FgEscape);
	public string Subtle => CachedEscape(nameof(Subtle), _theme.Subtle, FgEscape);
	public string Reset => "\x1b[0m";
	public string Accent => CachedEscape(nameof(Accent), _theme.Accent, FgEscape);
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

	public void SetDarkBackground() => Write($"\x1b]11;#{_theme.WindowBackground.R:X2}{_theme.WindowBackground.G:X2}{_theme.WindowBackground.B:X2}\x07");

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
		var length = 0;
		var span = s.AsSpan();

		while (!span.IsEmpty)
		{
			if (span[0] == '\x1b')
			{
				var end = span.IndexOf('m');

				span = end < 0 ? span[..0] : span[(end + 1)..];
				continue;
			}

			if (Rune.DecodeFromUtf16(span, out var rune, out var consumed) is not OperationStatus.Done)
			{
				length++;

				span = span[1..];
				continue;
			}

			length += DisplayCells.Width(rune);

			span = span[consumed..];
		}

		return length;
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

	public void ClearLine() => Console.Write("\r\x1b[2K");

	public void ClearToEndOfScreen() => Console.Write("\x1b[J");

	public void OnInterrupt(Action handler) => Console.CancelKeyPress += (_, args) =>
	{
		args.Cancel = true;
		handler();
	};

	private string GetColorEscape(TerminalColor color) =>
		CachedEscape("role:" + color, ResolveRoleColor(color), FgEscape);

	private RgbColor ResolveRoleColor(TerminalColor color)
	{
		if (typeof(ITheme).GetProperty(color.ToString())?.GetValue(_theme) is RgbColor resolved)
			return resolved;

		return _theme.Primary;
	}

	private string CachedEscape(string key, RgbColor color, Func<RgbColor, string> build)
	{
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
