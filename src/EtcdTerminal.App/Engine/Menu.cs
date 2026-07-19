using EtcdTerminal;
using EtcdTerminal.App.Modules;
using System.Text.RegularExpressions;

namespace EtcdTerminal.App.Engine;

public static class Menu
{
	private const string _arrow = "  ❯ ";
	private const string _emptyIndent = "    ";
	private const string _clearAnsi = "\x1b[J";

	public static string? Show(string title, IEnumerable<string> choices, Func<string, string>? displayConverter = null)
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
		{
			Console.Write(i == 0 ? _arrow : _emptyIndent);

			if (i == 0)
				Console.ForegroundColor = ConsoleColor.Yellow;

			WriteTruncated(plain[i]);

			if (i == 0)
				Console.ResetColor();

			Console.WriteLine();
		}

		var menuEnd = Console.CursorTop;
		StatusBar.Render();
		Console.CursorTop = menuEnd;
		Console.CursorLeft = 0;

		while (true)
		{
			var key = Console.ReadKey(true);
			var oldIndex = index;

			switch (key.Key)
			{
				case ConsoleKey.Escape:
					Console.CursorTop = menuStart;
					Console.CursorLeft = 0;
					Console.Write(_clearAnsi);
					return null;
				case ConsoleKey.Enter:
					Console.CursorTop = menuStart;
					Console.CursorLeft = 0;
					Console.Write(_clearAnsi);
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

			Console.CursorTop = firstItemTop + oldIndex;
			Console.CursorLeft = 0;
			Console.Write(_emptyIndent);

			WriteTruncated(plain[oldIndex]);

			Console.CursorTop = firstItemTop + index;
			Console.CursorLeft = 0;
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write(_arrow);

			WriteTruncated(plain[index]);

			Console.ResetColor();
		}
	}

	private static void WriteTruncated(string text)
	{
		var maxLen = Console.WindowWidth - 5;

		if (text.Length > maxLen)
		{
			Console.Write(text.AsSpan(0, maxLen - 3));
			Console.Write("...");
		}
		else
		{
			Console.Write(text);
		}
	}

	private static string StripMarkup(string text) => Regex.Replace(text, @"\[/?[^\]]*\]", "");
}
