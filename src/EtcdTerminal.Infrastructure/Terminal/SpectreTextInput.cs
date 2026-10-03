using EtcdTerminal.Presentation;
using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

public sealed class SpectreTextInput(EscapableConsole _console) : ITextInput
{
	private static readonly Style _promptStyle = new(decoration: Decoration.Bold);

	public string? ReadLine(string prompt, string? defaultValue = null)
	{
		_console.Cursor.Show(true);

		try
		{
			var textPrompt = new TextPrompt<string>(Markup.Escape(prompt))
				.PromptStyle(_promptStyle)
				.AllowEmpty();

			if (defaultValue is not null)
				textPrompt
					.DefaultValue(defaultValue)
					.EditableDefaultValue(true)
					.ShowDefaultValue(false);

			return _console.Prompt(textPrompt);
		}
		catch (OperationCanceledException)
		{
			return null;
		}
		finally
		{
			_console.Cursor.Show(false);
		}
	}

	public string? ReadSecret(string prompt)
	{
		_console.Cursor.Show(true);

		try
		{
			return _console.Prompt(new TextPrompt<string>(Markup.Escape(prompt))
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
			_console.Cursor.Show(false);
		}
	}
}
