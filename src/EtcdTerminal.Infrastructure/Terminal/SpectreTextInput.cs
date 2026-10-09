using EtcdTerminal.Presentation;
using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

public sealed class SpectreTextInput(EscapableConsole _console, RoleStyleMapper _styles) : ITextInput
{
	private Style PromptStyle => _styles.Resolve(TextRole.Primary).Decoration(Decoration.Bold);

	public string? ReadLine(string prompt, string? defaultValue = null)
	{
		_console.Cursor.Show(true);

		try
		{
			Margin();

			var textPrompt = new TextPrompt<string>(StyledPrompt(prompt))
				.PromptStyle(PromptStyle)
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

			return _console.Prompt(new TextPrompt<string>(StyledPrompt(prompt))
				.PromptStyle(PromptStyle)
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

	private string StyledPrompt(string prompt) => $"[{PromptStyle.ToMarkup()}]{Markup.Escape(prompt)}[/]";
}
