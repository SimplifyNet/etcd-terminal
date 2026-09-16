using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

public sealed class ScreenLayout(ITerminal _terminal, StatusBar _statusBar)
{
	public void RenderHeader()
	{
		_terminal.Clear();
		Header.Render(_terminal);

		var savedTop = _terminal.CursorTop;

		_statusBar.Render();
		_terminal.SetCursorPosition(0, savedTop);
	}
}
