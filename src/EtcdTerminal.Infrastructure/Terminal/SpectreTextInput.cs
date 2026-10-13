using EtcdTerminal.Presentation;
using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// Single-line text entry. The library's <c>TextPrompt</c> has no caret: it
/// appends and backspaces at the end only (0.57.2), so the line is edited
/// here with a <see cref="LineBuffer"/> and redrawn on its own row after
/// every key, the caret placed with the writer's cursor moves. Terminals
/// without ANSI keep the library prompt. Escape cancels through
/// <see cref="EscapableConsole"/>, as it did before.
/// </summary>
public sealed class SpectreTextInput(EscapableConsole _console, RoleStyleMapper _styles) : ITextInput
{
	private const char Mask = '*';

	private Style PromptStyle => _styles.Resolve(TextRole.Primary).Decoration(Decoration.Bold);

	private Style InputStyle => _styles.Resolve(TextRole.Primary);

	public string? ReadLine(string prompt, string? defaultValue = null) => Read(prompt, defaultValue ?? string.Empty, secret: false);

	public string? ReadSecret(string prompt) => Read(prompt, string.Empty, secret: true);

	private string? Read(string prompt, string initial, bool secret)
	{
		_console.Cursor.Show(true);

		try
		{
			return _console.Profile.Capabilities.Ansi
				? Edit(prompt, initial, secret)
				: Fallback(prompt, initial, secret);
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

	private string Edit(string prompt, string initial, bool secret)
	{
		var buffer = new LineBuffer(initial);
		var label = ContentIndent.Text + prompt + " ";

		Draw(label, buffer, secret);

		while (true)
		{
			var key = _console.Input.ReadKey(true);

			if (key is null)
				continue;

			if (key.Value.Key is ConsoleKey.Enter)
			{
				_console.WriteLine();

				return buffer.Text;
			}

			if (buffer.Apply(key.Value))
				Draw(label, buffer, secret);
		}
	}

	/// <summary>
	/// The row is rewritten from its first column: the label, then the part
	/// of the answer that fits beside it, scrolled so the caret stays in view,
	/// then the caret moved back to its position inside that part.
	/// </summary>
	private void Draw(string label, LineBuffer buffer, bool secret)
	{
		var text = secret ? new string(Mask, buffer.Length) : buffer.Text;
		var room = Math.Max(1, _console.Profile.Width - label.Length - 1);
		var skip = Math.Max(0, buffer.Cursor - room);
		var shown = text.Substring(skip, Math.Min(room, text.Length - skip));
		var back = shown.Length - (buffer.Cursor - skip);

		_console.WriteAnsi(writer =>
		{
			writer.CursorHorizontalAbsolute(1);
			writer.EraseInLine(2);
			writer.Write(label, PromptStyle, null);
			writer.Write(shown, InputStyle, null);

			if (back > 0)
				writer.CursorLeft(back);
		});
	}

	private string Fallback(string prompt, string initial, bool secret)
	{
		_console.Write(new Text(ContentIndent.Text));

		var textPrompt = new TextPrompt<string>($"[{PromptStyle.ToMarkup()}]{Markup.Escape(prompt)}[/]")
			.PromptStyle(PromptStyle)
			.AllowEmpty();

		if (secret)
			textPrompt.Secret();
		else if (initial.Length > 0)
			textPrompt
				.DefaultValue(initial)
				.EditableDefaultValue(true)
				.ShowDefaultValue(false);

		return _console.Prompt(textPrompt);
	}
}
