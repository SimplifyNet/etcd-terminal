using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class EtcdLightTheme : ITheme
{
	public string Id => "EtcdLight";
	public string Name => "Etcd";
	public string Variant => "Light";

	public RgbColor WindowBackground { get; } = new(255, 255, 255);
	public RgbColor BandBackground { get; } = new(237, 241, 245);
	public RgbColor ActionPanelTitleBackground { get; } = new(247, 249, 252);
	public RgbColor Primary { get; } = new(20, 24, 28);
	public RgbColor Secondary { get; } = new(0, 100, 100);
	public RgbColor Success { get; } = new(0, 107, 0);
	public RgbColor Danger { get; } = new(180, 35, 35);
	public RgbColor Warning { get; } = new(120, 85, 0);
	public RgbColor Muted { get; } = new(90, 101, 112);
	public RgbColor Subtle { get; } = new(75, 85, 96);
	public RgbColor Accent { get; } = new(65, 158, 218);
	public RgbColor Banner { get; } = new(65, 158, 218);
}
