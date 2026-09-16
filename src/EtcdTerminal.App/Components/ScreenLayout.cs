using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

public sealed class ScreenLayout(ITerminal _terminal, StatusBar _statusBar, Header _header)
{
	public void RenderHeader()
	{
		_terminal.Clear();
		_header.Render();

		var savedTop = _terminal.CursorTop;

		_statusBar.Render();
		_terminal.SetCursorPosition(0, savedTop);
	}
}
