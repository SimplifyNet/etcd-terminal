using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class SolarizedLightTheme : ITheme
{
	public string Id => "SolarizedLight";
	public string Name => "Solarized";
	public string Variant => "Light";

	public RgbColor WindowBackground { get; } = new(253, 246, 227);
	public RgbColor BandBackground { get; } = new(238, 232, 213);
	public RgbColor ActionPanelTitleBackground { get; } = new(247, 241, 225);
	public RgbColor Primary { get; } = new(72, 88, 94);
	public RgbColor Secondary { get; } = new(26, 105, 170);
	public RgbColor Success { get; } = new(85, 110, 0);
	public RgbColor Danger { get; } = new(185, 40, 37);
	public RgbColor Warning { get; } = new(125, 97, 0);
	public RgbColor Muted { get; } = new(88, 104, 110);
	public RgbColor Subtle { get; } = new(78, 92, 98);
	public RgbColor Accent { get; } = new(185, 70, 18);
	public RgbColor Banner { get; } = new(203, 75, 22);
}
