using System.Reflection;
using EtcdTerminal.Terminal;
using EtcdTerminal.Theming;
using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

public sealed class ConsoleTerminal(ITheme _theme) : ITerminal
{
	private readonly Dictionary<string, string> _escapeCache = new();

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

	public void ResetBackground() => Console.Write("\x1b]111\x07");

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

	public void OnInterrupt(Action handler) => Console.CancelKeyPress += (_, args) =>
	{
		args.Cancel = true;
		handler();
	};

	private void SetDarkBackground() => Write($"\x1b]11;#{_theme.WindowBackground.R:X2}{_theme.WindowBackground.G:X2}{_theme.WindowBackground.B:X2}\x07");

	private void ResetColor() => Console.ResetColor();

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
