using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

public sealed class ScreenLayout(ITerminalOutput _output, ITerminalCursor _cursor, StatusBar _statusBar, Header _header)
{
	public void RenderHeader()
	{
		_output.Clear();
		_header.Render();

		var savedTop = _cursor.CursorTop;

		_statusBar.Render();
		_cursor.SetCursorPosition(0, savedTop);
	}
}
