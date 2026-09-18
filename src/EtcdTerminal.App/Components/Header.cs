using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

public sealed class Header(ITerminalWidgets _terminal)
{
	public void Render() => _terminal.WriteBanner("etcd-terminal");
}
