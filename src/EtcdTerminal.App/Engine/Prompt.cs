using EtcdTerminal.Configuration;
using EtcdTerminal.Terminal;
using Spectre.Console;

namespace EtcdTerminal.App.Engine;

public sealed class Prompt(ITerminal _terminal)
{
	private static readonly Style _promptStyle = new(decoration: Decoration.Bold);

	private readonly IAnsiConsole _console = new EscapableConsole(AnsiConsole.Console);

	public string? Ask(string prompt, bool allowEmpty = false)
	{
		_terminal.SetCursorVisible(true);

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
			_terminal.SetCursorVisible(false);
		}
	}

	public string? Ask(string prompt, string defaultValue)
	{
		_terminal.SetCursorVisible(true);

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
			_terminal.SetCursorVisible(false);
		}
	}

	public string? Secret(string prompt)
	{
		_terminal.SetCursorVisible(true);

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
			_terminal.SetCursorVisible(false);
		}
	}

	public bool Confirm(string prompt)
	{
		_terminal.SetCursorVisible(true);

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
			_terminal.SetCursorVisible(false);
		}
	}
}
