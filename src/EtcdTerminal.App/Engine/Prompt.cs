using Spectre.Console;

namespace EtcdTerminal.App.Engine;

public static class Prompt
{
	private static readonly Style _promptStyle = new(decoration: Decoration.Bold);

	private static readonly IAnsiConsole _console = new EscapableConsole(AnsiConsole.Console);

	public static string? Ask(string prompt)
	{
		try
		{
			var input = _console.Prompt(new TextPrompt<string>(prompt)
				.PromptStyle(_promptStyle)
				.AllowEmpty());

			return string.IsNullOrWhiteSpace(input) ? null : input;
		}
		catch (OperationCanceledException)
		{
			return null;
		}
	}

	public static string? Ask(string prompt, string defaultValue)
	{
		try
		{
			return _console.Prompt(new TextPrompt<string>(prompt)
				.PromptStyle(_promptStyle)
				.AllowEmpty()
				.DefaultValue(defaultValue)
				.EditableDefaultValue(true)
				.ShowDefaultValue(false));
		}
		catch (OperationCanceledException)
		{
			return null;
		}
	}

	public static string? Secret(string prompt)
	{
		try
		{
			return _console.Prompt(new TextPrompt<string>(prompt)
				.PromptStyle(_promptStyle)
				.Secret()
				.AllowEmpty());
		}
		catch (OperationCanceledException)
		{
			return null;
		}
	}

	public static bool Confirm(string prompt)
	{
		try
		{
			return _console.Confirm(prompt, false);
		}
		catch (OperationCanceledException)
		{
			return false;
		}
	}
}
