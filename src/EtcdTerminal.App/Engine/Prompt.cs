using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Engine;

public sealed class Prompt(ITerminalOutput _output, ITerminalCursor _cursor, ITerminalStyle _style, ITextInput _textInput, StatusBar _statusBar, IAppSettingsStore _settings)
{
	public string? Ask(string prompt, bool allowEmpty = false)
	{
		_cursor.SetCursorVisible(true);

		try
		{
			_statusBar.EnsureCursorAboveBar();
			_output.Write(_style.Indent);

			var input = Read(() => _textInput.ReadLine(prompt));

			if (input is null)
				return null;

			if (_settings.Current.TrimInputValues)
				input = input.Trim();

			if (string.IsNullOrWhiteSpace(input))
				return allowEmpty ? string.Empty : null;

			return input;
		}
		finally
		{
			_cursor.SetCursorVisible(false);
		}
	}

	public string? Ask(string prompt, string defaultValue)
	{
		_cursor.SetCursorVisible(true);

		try
		{
			_statusBar.EnsureCursorAboveBar();
			_output.Write(_style.Indent);

			var input = Read(() => _textInput.ReadLine(prompt, defaultValue));

			if (input is null)
				return null;

			return _settings.Current.TrimInputValues ? input.Trim() : input;
		}
		finally
		{
			_cursor.SetCursorVisible(false);
		}
	}

	public string? Secret(string prompt)
	{
		_cursor.SetCursorVisible(true);

		try
		{
			_statusBar.EnsureCursorAboveBar();
			_output.Write(_style.Indent);

			return Read(() => _textInput.ReadSecret(prompt));
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
