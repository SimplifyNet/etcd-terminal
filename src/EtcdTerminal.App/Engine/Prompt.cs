using System.Text;
using Spectre.Console;

namespace EtcdTerminal.App.Engine;

public static class Prompt
{
	private const string PromptFormat = "[bold]{0}[/] ";
	private const string ConfirmFormat = "[bold]{0}[/] [grey][[y/N]][/] ";

	public static string? Ask(string prompt)
	{
		AnsiConsole.Markup(PromptFormat, Markup.Escape(prompt));

		return ReadLine();
	}

	public static string? Ask(string prompt, string defaultValue)
	{
		AnsiConsole.Markup(PromptFormat, Markup.Escape(prompt));

		var input = ReadLine(defaultValue);

		return input;
	}

	public static string? Secret(string prompt)
	{
		AnsiConsole.Markup(PromptFormat, Markup.Escape(prompt));

		return ReadSecret();
	}

	public static bool? Confirm(string prompt)
	{
		AnsiConsole.Markup(ConfirmFormat, Markup.Escape(prompt));

		while (true)
		{
			var key = Console.ReadKey(true);

			switch (key.Key)
			{
				case ConsoleKey.Escape:
					Console.WriteLine();
					return null;
				case ConsoleKey.Enter:
					Console.WriteLine("n");
					return false;
				default:
					if (key.KeyChar is 'y' or 'Y')
					{
						Console.WriteLine("y");
						return true;
					}
					if (key.KeyChar is 'n' or 'N')
					{
						Console.WriteLine("n");
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
		var startCol = Console.CursorLeft;

		if (!string.IsNullOrEmpty(prefill))
			Console.Write(prefill);

		while (true)
		{
			var key = Console.ReadKey(true);

			switch (key.Key)
			{
				case ConsoleKey.Escape:
					ClearInput(startCol);
					Console.WriteLine();
					return null;
				case ConsoleKey.Enter:
					Console.WriteLine();
					return input.ToString();
				case ConsoleKey.LeftArrow:
					if (cursor > 0)
					{
						cursor--;
						Console.CursorLeft--;
					}
					break;
				case ConsoleKey.RightArrow:
					if (cursor < input.Length)
					{
						Console.Write(input[cursor]);
						cursor++;
					}
					break;
				case ConsoleKey.Home:
					Console.CursorLeft = startCol;
					cursor = 0;
					break;
				case ConsoleKey.End:
					Console.CursorLeft = startCol + input.Length;
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
						RedrawInput(startCol, input.ToString(), cursor + 1);
						cursor++;
					}
					break;
			}
		}
	}

	private static void ClearInput(int startCol)
	{
		var endCol = Console.CursorLeft;

		Console.CursorLeft = startCol;
		Console.Write(new string(' ', Math.Max(0, endCol - startCol + 1)));
		Console.CursorLeft = startCol;
	}

	private static void RedrawInput(int startCol, string text, int cursorPos)
	{
		Console.CursorLeft = startCol;
		Console.Write(text + ' ');
		Console.CursorLeft = startCol + cursorPos;
	}

	private static string? ReadSecret()
	{
		var input = new StringBuilder();

		while (true)
		{
			var key = Console.ReadKey(true);

			switch (key.Key)
			{
				case ConsoleKey.Escape:
					Console.WriteLine();
					return null;
				case ConsoleKey.Enter:
					Console.WriteLine();
					return input.ToString();
				case ConsoleKey.Backspace:
					if (input.Length > 0)
					{
						input.Length--;
						Console.Write("\b \b");
					}
					break;
				default:
					if (!char.IsControl(key.KeyChar))
					{
						input.Append(key.KeyChar);
						Console.Write('*');
					}
					break;
			}
		}
	}
}
