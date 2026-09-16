using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

public sealed class Message(ITerminal _terminal, PressAnyKeyPrompt _pressAnyKey)
{
	public void ShowSuccess(string text) => Show(text, TerminalColor.Success);

	public void ShowError(string text) => Show(text, TerminalColor.Error);

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
		_terminal.WriteLine();
		_terminal.WriteIndentedLine(text, color);
		_terminal.WriteLine();

		_pressAnyKey.Show();
	}
}
