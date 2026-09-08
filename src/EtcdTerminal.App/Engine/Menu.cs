using EtcdTerminal.Terminal;
using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using System.Text.RegularExpressions;

namespace EtcdTerminal.App.Engine;

public sealed partial class Menu(ITerminal _terminal, StatusBar _statusBar)
{
	private const string ClearToEndOfScreen = "\x1b[J";

	public string? Show(string title, IEnumerable<string> choices, Func<string, string>? displayConverter = null, EtcdConnectionConfig? config = null)
	{
		var items = choices.ToList();
		var index = 0;

		var plain = items.Select(c =>
		{
			var formatted = displayConverter?.Invoke(c) ?? c;

			return StripMarkup(formatted);
		}).ToList();

		var menuStart = _terminal.CursorTop;

		_terminal.ResetColor();

		if (!string.IsNullOrEmpty(title))
		{
			_terminal.WriteLine();
			_terminal.WriteLine($"{_terminal.SelectionPointerEmpty}{title}");
			_terminal.WriteLine();
		}

		var firstItemTop = _terminal.CursorTop;

		for (var i = 0; i < items.Count; i++)
			DrawItem(firstItemTop + i, plain[i], i == index);

		var menuEnd = _terminal.CursorTop;

		_statusBar.Render(config);
		_terminal.SetCursorPosition(0, menuEnd);

		while (true)
		{
			var key = _terminal.ReadKey();
			var oldIndex = index;

			switch (key.Key)
			{
				case ConsoleKey.Escape:
					ClearMenu(menuStart);
					return null;
				case ConsoleKey.Enter:
					ClearMenu(menuStart);
					return items[index];
				case ConsoleKey.UpArrow:
					index = (index - 1 + items.Count) % items.Count;
					break;
				case ConsoleKey.DownArrow:
					index = (index + 1) % items.Count;
					break;
				default:
					continue;
			}

			DrawItem(firstItemTop + oldIndex, plain[oldIndex], false);
			DrawItem(firstItemTop + index, plain[index], true);
		}
	}

	private void ClearMenu(int menuStart)
	{
		_terminal.SetCursorPosition(0, menuStart);
		_terminal.Write(ClearToEndOfScreen);
	}

	private void DrawItem(int top, string text, bool isSelected)
	{
		_terminal.SetCursorPosition(0, top);
		_terminal.ResetColor();

		_terminal.Write(isSelected ? _terminal.Accent : string.Empty);
		_terminal.Write(isSelected ? _terminal.SelectionPointer : _terminal.SelectionPointerEmpty);

		WriteTruncated(text);
		_terminal.ResetColor();
	}

	private void WriteTruncated(string text)
	{
		var maxLen = _terminal.WindowWidth - _terminal.SelectionPointer.Length - 1;

		if (text.Length > maxLen)
		{
			_terminal.Write(text.AsSpan(0, Math.Max(0, maxLen - 3)).ToString());
			_terminal.Write("...");
		}
		else
			_terminal.Write(text);
	}

	private static string StripMarkup(string text) => MarkupPattern().Replace(text, string.Empty);

	[GeneratedRegex(@"\[/?[^\]]*\]")]
	private static partial Regex MarkupPattern();
}
