using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class EtcdBlueTheme : ITheme
{
	public string Id => "EtcdBlue";
	public string Name => "Etcd Blue";
	public string Variant => "Black";

	public RgbColor WindowBackground { get; } = new(5, 18, 35);
	public RgbColor BandBackground { get; } = new(13, 35, 59);
	public RgbColor ActionPanelTitleBackground { get; } = new(9, 27, 47);
	public RgbColor Primary { get; } = new(240, 247, 255);
	public RgbColor Secondary { get; } = new(100, 190, 235);
	public RgbColor Success { get; } = new(75, 205, 145);
	public RgbColor Danger { get; } = new(245, 90, 100);
	public RgbColor Warning { get; } = new(255, 195, 75);
	public RgbColor Muted { get; } = new(135, 160, 185);
	public RgbColor Subtle { get; } = new(75, 105, 135);
	public RgbColor Accent { get; } = new(65, 158, 218);
	public RgbColor Banner { get; } = new(65, 158, 218);
}