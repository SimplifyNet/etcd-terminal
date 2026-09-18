using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Engine;

public sealed class Prompt(ITerminal _terminal, ITextInput _textInput, StatusBar _statusBar)
{
	public string? Ask(string prompt, bool allowEmpty = false)
	{
		_terminal.SetCursorVisible(true);

		try
		{
			_statusBar.EnsureCursorAboveBar();
			_terminal.Write(_terminal.Indent);

			var input = Read(() => _textInput.ReadLine(prompt));

			if (input is null)
				return null;

			if (AppSettingsStore.Current.TrimInputValues)
				input = input.Trim();

			if (string.IsNullOrWhiteSpace(input))
				return allowEmpty ? string.Empty : null;

			return input;
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
			_statusBar.EnsureCursorAboveBar();
			_terminal.Write(_terminal.Indent);

			var input = Read(() => _textInput.ReadLine(prompt, defaultValue));

			if (input is null)
				return null;

			return AppSettingsStore.Current.TrimInputValues ? input.Trim() : input;
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
			_statusBar.EnsureCursorAboveBar();
			_terminal.Write(_terminal.Indent);

			return Read(() => _textInput.ReadSecret(prompt));
		}
		finally
		{
			_terminal.SetCursorVisible(false);
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
