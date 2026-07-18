using System.Text.RegularExpressions;

namespace EtcdTerminal.Console.Engine;

public static class Menu
{
	public static string? Show(string title, IEnumerable<string> choices, Func<string, string>? displayConverter = null)
	{
		var items = choices.ToList();
		var index = 0;

		var plain = items.Select(c =>
		{
			var formatted = displayConverter?.Invoke(c) ?? c;

			return StripMarkup(formatted);
		}).ToList();

		var menuStart = System.Console.CursorTop;

		System.Console.ResetColor();

		if (!string.IsNullOrEmpty(title))
		{
			System.Console.WriteLine();
			System.Console.WriteLine(title);
			System.Console.WriteLine();
		}

		var firstItemTop = System.Console.CursorTop;

		for (var i = 0; i < items.Count; i++)
		{
			System.Console.Write(i == 0 ? "  > " : "    ");

			if (i == 0)
				System.Console.ForegroundColor = ConsoleColor.Yellow;

			WriteTruncated(plain[i]);

			if (i == 0)
				System.Console.ResetColor();

			System.Console.WriteLine();
		}

		System.Console.WriteLine();
		System.Console.ForegroundColor = ConsoleColor.DarkGray;
		System.Console.Write("(\u2191/\u2193 navigate, Enter confirm, Esc back)");
		System.Console.ResetColor();

		while (true)
		{
			var key = System.Console.ReadKey(true);
			var oldIndex = index;

			switch (key.Key)
			{
				case ConsoleKey.Escape:
					System.Console.CursorTop = menuStart;
					System.Console.CursorLeft = 0;
					System.Console.Write("\x1b[J");
					return null;
				case ConsoleKey.Enter:
					System.Console.CursorTop = menuStart;
					System.Console.CursorLeft = 0;
					System.Console.Write("\x1b[J");
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

			System.Console.CursorTop = firstItemTop + oldIndex;
			System.Console.CursorLeft = 0;
			System.Console.Write("    ");

			WriteTruncated(plain[oldIndex]);

			System.Console.CursorTop = firstItemTop + index;
			System.Console.CursorLeft = 0;
			System.Console.ForegroundColor = ConsoleColor.Yellow;
			System.Console.Write("  > ");

			WriteTruncated(plain[index]);

			System.Console.ResetColor();
		}
	}

	private static void WriteTruncated(string text)
	{
		var maxLen = System.Console.WindowWidth - 5;

		if (text.Length > maxLen)
		{
			System.Console.Write(text.AsSpan(0, maxLen - 3));
			System.Console.Write("...");
		}
		else
		{
			System.Console.Write(text);
		}
	}

	private static string StripMarkup(string text) => Regex.Replace(text, @"\[/?[^\]]*\]", "");
}
