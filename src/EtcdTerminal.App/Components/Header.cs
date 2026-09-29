using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Components;

/// <summary>
/// The application banner. Screens place it inside their frame; there is no
/// inline variant, because the host owns every write to the console.
/// </summary>
public sealed class Header
{
	private const string BannerText = "etcd-terminal";

	public PanelModel BuildModel() =>
		new([new PanelLine([new StyledText(BannerText, TextRole.Primary)])], PanelKind.Banner);
}
