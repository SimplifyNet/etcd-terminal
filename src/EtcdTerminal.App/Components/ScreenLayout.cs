using EtcdTerminal.Terminal;
using EtcdTerminal.Configuration;

namespace EtcdTerminal.App.Components;

public sealed class ScreenLayout(ITerminal _terminal, StatusBar _statusBar)
{
	public void RenderHeader(EtcdConnectionConfig? config)
	{
		_terminal.Clear();
		Header.Render();

		var savedTop = _terminal.CursorTop;

		_statusBar.Render(config);
		_terminal.SetCursorPosition(0, savedTop);
	}
}
