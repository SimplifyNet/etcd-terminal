using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

public static class Header
{
	public static void Render(ITerminal terminal) =>
		terminal.WriteBanner("etcd-terminal");
}
