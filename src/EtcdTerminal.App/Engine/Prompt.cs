using EtcdTerminal.Configuration;
using EtcdTerminal.Terminal;
using Spectre.Console;

namespace EtcdTerminal.App.Engine;

public static class Prompt
{
	private static readonly Style _promptStyle = new(decoration: Decoration.Bold);

	private static readonly IAnsiConsole _console = new EscapableConsole(AnsiConsole.Console);

	public static string? Ask(ITerminal terminal, string prompt, bool allowEmpty = false)
	{
		terminal.SetCursorVisible(true);

		try
		{
			var input = _console.Prompt(new TextPrompt<string>(prompt)
				.PromptStyle(_promptStyle)
				.AllowEmpty());

			if (AppSettingsStore.Current.TrimInputValues)
				input = input.Trim();

			if (string.IsNullOrWhiteSpace(input))
				return allowEmpty ? string.Empty : null;

			return input;
		}
		catch (OperationCanceledException)
		{
			return null;
		}
		finally
		{
			terminal.SetCursorVisible(false);
		}
	}

	public static string? Ask(ITerminal terminal, string prompt, string defaultValue)
	{
		terminal.SetCursorVisible(true);

		try
		{
			var input = _console.Prompt(new TextPrompt<string>(prompt)
				.PromptStyle(_promptStyle)
				.AllowEmpty()
				.DefaultValue(defaultValue)
				.EditableDefaultValue(true)
				.ShowDefaultValue(false));

			return AppSettingsStore.Current.TrimInputValues ? input.Trim() : input;
		}
		catch (OperationCanceledException)
		{
			return null;
		}
		finally
		{
			terminal.SetCursorVisible(false);
		}
	}

	public static string? Secret(ITerminal terminal, string prompt)
	{
		terminal.SetCursorVisible(true);

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
		finally
		{
			terminal.SetCursorVisible(false);
		}
	}

	public static bool Confirm(ITerminal terminal, string prompt)
	{
		terminal.SetCursorVisible(true);

		try
		{
			return _console.Confirm(prompt, false);
		}
		catch (OperationCanceledException)
		{
			return false;
		}
		finally
		{
			terminal.SetCursorVisible(false);
		}
	}
}
