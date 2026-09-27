using EtcdTerminal.App.Components;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Engine;

/// <summary>
/// Generic input mechanics: cancellation versus validation. Trimming and empty
/// policies come from the caller, which owns the application settings.
/// </summary>
public sealed class Prompt(ITerminalOutput _output, ITerminalCursor _cursor, ITerminalStyle _style, ITextInput _textInput, StatusBar _statusBar)
{
	public string? Ask(string prompt, bool allowEmpty = false, bool trim = true)
	{
		_cursor.SetCursorVisible(true);

		try
		{
			_statusBar.EnsureCursorAboveBar();
			_output.Write(_style.Indent);

			var input = Read(() => _textInput.ReadLine(prompt));

			if (input is null)
				return null;

			if (trim)
				input = input.Trim();

			if (input.Length == 0)
				return allowEmpty ? string.Empty : null;

			return input;
		}
		finally
		{
			_cursor.SetCursorVisible(false);
		}
	}

	public string? Ask(string prompt, string defaultValue, bool trim = true)
	{
		_cursor.SetCursorVisible(true);

		try
		{
			_statusBar.EnsureCursorAboveBar();
			_output.Write(_style.Indent);

			var input = Read(() => _textInput.ReadLine(prompt, defaultValue));

			if (input is null)
				return null;

			return trim ? input.Trim() : input;
		}
		finally
		{
			_cursor.SetCursorVisible(false);
		}
	}

	public string? Secret(string prompt, bool trim = true)
	{
		_cursor.SetCursorVisible(true);

		try
		{
			_statusBar.EnsureCursorAboveBar();
			_output.Write(_style.Indent);

			var input = Read(() => _textInput.ReadSecret(prompt));

			if (input is null)
				return null;

			return trim ? input.Trim() : input;
		}
		finally
		{
			_cursor.SetCursorVisible(false);
		}
	}

	private string? Read(Func<string?> read)
	{
		_statusBar.RenderPreservingCursor();

		var input = read();

		_statusBar.RenderPreservingCursor();

		return input;
	}
}
