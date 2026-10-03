using EtcdTerminal.Presentation.Terminal;

namespace EtcdTerminal.App.Engine;

/// <summary>
/// Generic input mechanics: cancellation versus validation. Trimming and empty
/// policies come from the caller, which owns the application settings.
/// </summary>
public sealed class Prompt(ITextInput _textInput)
{
	public string? Ask(string prompt, bool allowEmpty = false, bool trim = true)
	{
		var input = _textInput.ReadLine(prompt);

		if (input is null)
			return null;

		if (trim)
			input = input.Trim();

		if (input.Length == 0)
			return allowEmpty ? string.Empty : null;

		return input;
	}

	public string? Ask(string prompt, string defaultValue, bool trim = true)
	{
		var input = _textInput.ReadLine(prompt, defaultValue);

		if (input is null)
			return null;

		return trim ? input.Trim() : input;
	}

	public string? Secret(string prompt, bool trim = true)
	{
		var input = _textInput.ReadSecret(prompt);

		if (input is null)
			return null;

		return trim ? input.Trim() : input;
	}
}
