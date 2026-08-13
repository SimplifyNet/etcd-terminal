using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using System.Text.RegularExpressions;

namespace EtcdTerminal.App.Engine;

public static class Menu
{
	private const string ClearToEndOfScreen = "\x1b[J";

	public static string? Show(string title, IEnumerable<string> choices, Func<string, string>? displayConverter = null, EtcdConnectionConfig? config = null)
	{
		var items = choices.ToList();
		var index = 0;

		var plain = items.Select(c =>
		{
			var formatted = displayConverter?.Invoke(c) ?? c;

			return StripMarkup(formatted);
		}).ToList();

		var menuStart = Console.CursorTop;

		Console.ResetColor();

		if (!string.IsNullOrEmpty(title))
		{
			Console.WriteLine();
			Console.WriteLine(title);
			Console.WriteLine();
		}

		var firstItemTop = Console.CursorTop;

		for (var i = 0; i < items.Count; i++)
			DrawItem(firstItemTop + i, plain[i], i == index);

		var menuEnd = Console.CursorTop;
		StatusBar.Render(config);
		Console.CursorTop = menuEnd;
		Console.CursorLeft = 0;

		while (true)
		{
			var key = Console.ReadKey(true);
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

	private static void ClearMenu(int menuStart)
	{
		Console.CursorTop = menuStart;
		Console.CursorLeft = 0;
		Console.Write(ClearToEndOfScreen);
	}

	private static void DrawItem(int top, string text, bool isSelected)
	{
		Console.CursorTop = top;
		Console.CursorLeft = 0;
		Console.ResetColor();

		Console.Write(isSelected ? TerminalPanel.Accent : string.Empty);
		Console.Write(isSelected ? TerminalPanel.SelectionPointer : TerminalPanel.SelectionPointerEmpty);

		WriteTruncated(text);
		Console.ResetColor();
	}

	private static void WriteTruncated(string text)
	{
		var maxLen = Console.WindowWidth - TerminalPanel.SelectionPointer.Length - 1;

		if (text.Length > maxLen)
		{
			Console.Write(text.AsSpan(0, Math.Max(0, maxLen - 3)));
			Console.Write("...");
		}
		else
		{
			Console.Write(text);
		}
	}

	private static string StripMarkup(string text) => Regex.Replace(text, @"\[/?[^\]]*\]", "");
}
