using EtcdTerminal.Presentation;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

/// <summary>
/// The application banner. A screen that owns the console places the banner
/// inside its frame; <see cref="Render"/> remains for screens that still draw
/// inline.
/// </summary>
public sealed class Header(ITerminalWidgets _terminal)
{
	private const string BannerText = "etcd-terminal";

	public PanelModel BuildModel() =>
		new([new PanelLine([new StyledText(BannerText, TextRole.Primary)])], PanelKind.Banner);

	public void Render() => _terminal.WriteBanner(BannerText);
}
