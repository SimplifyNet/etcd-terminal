using EtcdTerminal.Terminal;
using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

public sealed class SpectreTextInput : ITextInput
{
	private static readonly Style _promptStyle = new(decoration: Decoration.Bold);

	private readonly IAnsiConsole _console = new EscapableConsole(AnsiConsole.Console);

	public string? ReadLine(string prompt, string? defaultValue = null)
	{
		try
		{
			var textPrompt = new TextPrompt<string>(prompt)
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
	}

	public string? ReadSecret(string prompt)
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
}
