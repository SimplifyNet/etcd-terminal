using System.Text;
using Spectre.Console;

namespace EtcdTerminal.Console.Engine;

public static class Prompt
{
	public static string? Ask(string prompt)
	{
		AnsiConsole.Markup($"[bold]{Markup.Escape(prompt)}[/] ");

		return ReadLine();
	}

	public static string? Ask(string prompt, string defaultValue)
	{
		AnsiConsole.Markup($"[bold]{Markup.Escape(prompt)}[/] ");

		var input = ReadLine(defaultValue);

		return input ?? defaultValue;
	}

	public static string? Secret(string prompt)
	{
		AnsiConsole.Markup($"[bold]{Markup.Escape(prompt)}[/] ");

		return ReadSecret();
	}

	public static bool? Confirm(string prompt)
	{
		AnsiConsole.Markup($"[bold]{Markup.Escape(prompt)}[/] [grey][y/N][/] ");

		while (true)
		{
			var key = System.Console.ReadKey(true);

			switch (key.Key)
			{
				case ConsoleKey.Escape:
					System.Console.WriteLine();
					return null;
				case ConsoleKey.Enter:
					System.Console.WriteLine("n");
					return false;
				default:
					if (key.KeyChar is 'y' or 'Y')
					{
						System.Console.WriteLine("y");
						return true;
					}
					if (key.KeyChar is 'n' or 'N')
					{
						System.Console.WriteLine("n");
						return false;
					}
					break;
			}
		}
	}

	private static string? ReadLine() => ReadLine(null);

	private static string? ReadLine(string? prefill)
	{
		var input = new StringBuilder(prefill ?? "");
		var cursor = input.Length;
		var startCol = System.Console.CursorLeft;

		if (!string.IsNullOrEmpty(prefill))
			System.Console.Write(prefill);

		while (true)
		{
			var key = System.Console.ReadKey(true);

			switch (key.Key)
			{
				case ConsoleKey.Escape:
					ClearInput(startCol);
					System.Console.WriteLine();
					return null;
				case ConsoleKey.Enter:
					System.Console.WriteLine();
					return input.ToString();
				case ConsoleKey.LeftArrow:
					if (cursor > 0)
					{
						cursor--;
						System.Console.CursorLeft--;
					}
					break;
				case ConsoleKey.RightArrow:
					if (cursor < input.Length)
					{
						System.Console.Write(input[cursor]);
						cursor++;
					}
					break;
				case ConsoleKey.Home:
					System.Console.CursorLeft = startCol;
					cursor = 0;
					break;
				case ConsoleKey.End:
					System.Console.CursorLeft = startCol + input.Length;
					cursor = input.Length;
					break;
				case ConsoleKey.Backspace:
					if (cursor > 0)
					{
						input.Remove(cursor - 1, 1);
						cursor--;
						RedrawInput(startCol, input.ToString(), cursor);
					}
					break;
				case ConsoleKey.Delete:
					if (cursor < input.Length)
					{
						input.Remove(cursor, 1);
						RedrawInput(startCol, input.ToString(), cursor);
					}
					break;
				default:
					if (!char.IsControl(key.KeyChar))
					{
						input.Insert(cursor, key.KeyChar);
						RedrawInput(startCol, input.ToString(), cursor);
						cursor++;
					}
					break;
			}
		}
	}

	private static void ClearInput(int startCol)
	{
		var endCol = System.Console.CursorLeft;

		System.Console.CursorLeft = startCol;
		System.Console.Write(new string(' ', Math.Max(0, endCol - startCol + 1)));
		System.Console.CursorLeft = startCol;
	}

	private static void RedrawInput(int startCol, string text, int cursorPos)
	{
		System.Console.CursorLeft = startCol;
		System.Console.Write(text + ' ');
		System.Console.CursorLeft = startCol + cursorPos;
	}

	private static string? ReadSecret()
	{
		var input = new StringBuilder();

		while (true)
		{
			var key = System.Console.ReadKey(true);

			switch (key.Key)
			{
				case ConsoleKey.Escape:
					System.Console.WriteLine();
					return null;
				case ConsoleKey.Enter:
					System.Console.WriteLine();
					return input.ToString();
				case ConsoleKey.Backspace:
					if (input.Length > 0)
					{
						input.Length--;
						System.Console.Write("\b \b");
					}
					break;
				default:
					if (!char.IsControl(key.KeyChar))
					{
						input.Append(key.KeyChar);
						System.Console.Write('*');
					}
					break;
			}
		}
	}
}
