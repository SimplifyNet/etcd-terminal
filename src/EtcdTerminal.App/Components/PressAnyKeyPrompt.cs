using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

public sealed class PressAnyKeyPrompt(ITerminal _terminal, StatusBar _statusBar)
{
	public void Show()
	{
		_statusBar.EnsureCursorAboveBar(2);
		_terminal.WriteIndentedLine(LocalizationStore.Current.PressAnyKey, TerminalColor.Muted);
		_statusBar.RenderPreservingCursor();
		_terminal.ReadKey();
	}
}
