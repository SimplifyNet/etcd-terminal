using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

public sealed class PressAnyKeyPrompt(ITerminalOutput _output, ITerminalInput _input, StatusBar _statusBar, ILocalization _localization)
{
	public void Show()
	{
		_statusBar.EnsureCursorAboveBar(2);
		_output.WriteIndentedLine(_localization.PressAnyKey, TerminalColor.Muted);
		_statusBar.RenderPreservingCursor();
		_input.ReadKey();
	}
}
