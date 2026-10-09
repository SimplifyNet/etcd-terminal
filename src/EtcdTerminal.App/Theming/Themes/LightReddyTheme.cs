using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class LightReddyTheme : ITheme
{
	public string Id => "LightReddy";
	public string Name => "Light Reddy";
	public string Variant => "Light";

	public RgbColor WindowBackground { get; } = new(240, 240, 240);
	public RgbColor BandBackground { get; } = new(228, 227, 225);
	public RgbColor ActionPanelTitleBackground { get; } = new(234, 233, 231);
	public RgbColor Primary { get; } = new(0, 0, 0);
	public RgbColor Secondary { get; } = new(0, 180, 180);
	public RgbColor Success { get; } = new(0, 150, 0);
	public RgbColor Danger { get; } = new(220, 50, 50);
	public RgbColor Warning { get; } = new(198, 155, 0);
	public RgbColor Muted { get; } = new(128, 128, 128);
	public RgbColor Subtle { get; } = new(80, 80, 80);
	public RgbColor Accent { get; } = new(220, 95, 51);
	public RgbColor Banner { get; } = new(255, 95, 0);
}