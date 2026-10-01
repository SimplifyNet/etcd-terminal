using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation.Terminal;

namespace EtcdTerminal.App.Components;

/// <summary>
/// Writes an outcome below whatever the screen just did instead of opening a
/// frame of its own, so the values the user typed stay on screen next to the
/// result. Rows the released frame painted underneath are erased first, the
/// status bar is held out of the way while the message streams, and the footer
/// is put back on its row afterwards; the frame itself is never redrawn here.
/// </summary>
public sealed class Message(ITerminal _terminal, StatusBar _statusBar, ILocalization _localization)
{
	public void ShowSuccess(string text) => Show(text, TerminalColor.Success);

	public void ShowError(string text) => Show(text, TerminalColor.Danger);

	public void ShowWarning(string text) => Show(text, TerminalColor.Warning);

	public void ShowResult(bool ok, string success, string failure)
	{
		if (ok)
			ShowSuccess(success);
		else
			ShowError(failure);
	}

	private void Show(string text, TerminalColor color)
	{
		var lines = text.Split('\n');

		// The blank line above, the blank line below and the press-any-key label
		// follow the message, so the room is asked for in one go rather than one
		// row at a time: a message that does not fit then scrolls while it is
		// still being written instead of running onto the footer.
		_statusBar.EnsureRoomAbove(lines.Length + 3);
		_statusBar.ClearBelow();

		_terminal.WriteLine();

		foreach (var line in lines)
			_terminal.WriteIndentedLine(line.TrimEnd('\r'), color);

		_terminal.WriteLine();
		_terminal.WriteIndentedLine(_localization.PressAnyKey, TerminalColor.Muted);

		_statusBar.RenderPreservingCursor();

		_terminal.ReadKey();
	}
}
