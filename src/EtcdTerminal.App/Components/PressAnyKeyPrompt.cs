using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

public sealed class PressAnyKeyPrompt(ITerminal _terminal)
{
	public void Show()
	{
		_terminal.WriteLine(LocalizationStore.Current.PressAnyKey, TerminalColor.Muted);
		_terminal.ReadKey();
	}
}
