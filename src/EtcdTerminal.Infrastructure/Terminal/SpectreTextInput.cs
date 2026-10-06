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
			Margin();

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
			Margin();

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

	/// <summary>
	/// The four columns the prompt line was margined with before the
	/// migration. The widget trims the leading spaces of its own prompt, so
	/// the margin is written before it takes over the line; the widget clears
	/// that whole line when the answer is accepted.
	/// </summary>
	private void Margin() => _console.Write(new Text(ContentIndent.Text));
}
