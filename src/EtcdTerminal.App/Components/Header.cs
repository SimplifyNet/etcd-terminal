using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

public sealed class Header(ITerminal _terminal)
{
	public void Render() => _terminal.WriteBanner("etcd-terminal");
}
