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
		AnsiConsole.Markup($"[bold]{Markup.Escape(prompt)}[/] [grey]({defaultValue})[/] ");

		var input = ReadLine();

		return string.IsNullOrEmpty(input) ? defaultValue : input;
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

	private static string? ReadLine()
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
						System.Console.Write(key.KeyChar);
					}
					break;
			}
		}
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
