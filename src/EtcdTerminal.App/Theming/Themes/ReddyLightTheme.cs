using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class ReddyLightTheme : ITheme
{
	public string Id => "ReddyLight";
	public string Name => "Reddy";
	public string Variant => "Light";

	public RgbColor WindowBackground { get; } = new(240, 240, 240);
	public RgbColor BandBackground { get; } = new(228, 227, 225);
	public RgbColor ActionPanelTitleBackground { get; } = new(234, 233, 231);
	public RgbColor Primary { get; } = new(0, 0, 0);
	public RgbColor Secondary { get; } = new(0, 96, 96);
	public RgbColor Success { get; } = new(0, 110, 0);
	public RgbColor Danger { get; } = new(180, 35, 35);
	public RgbColor Warning { get; } = new(120, 85, 0);
	public RgbColor Muted { get; } = new(100, 100, 100);
	public RgbColor Subtle { get; } = new(80, 80, 80);
	public RgbColor Accent { get; } = new(220, 95, 51);
	public RgbColor Banner { get; } = new(255, 95, 0);
}
