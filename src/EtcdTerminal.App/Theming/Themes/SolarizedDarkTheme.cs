using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class SolarizedDarkTheme : ITheme
{
	public string Id => "SolarizedDark";
	public string Name => "Solarized";
	public string Variant => "Dark";

	public RgbColor WindowBackground { get; } = new(0, 43, 54);
	public RgbColor BandBackground { get; } = new(7, 54, 66);
	public RgbColor ActionPanelTitleBackground { get; } = new(3, 48, 60);
	public RgbColor Primary { get; } = new(147, 161, 161);
	public RgbColor Secondary { get; } = new(42, 161, 152);
	public RgbColor Success { get; } = new(133, 153, 0);
	public RgbColor Danger { get; } = new(240, 100, 94);
	public RgbColor Warning { get; } = new(181, 137, 0);
	public RgbColor Muted { get; } = new(131, 148, 150);
	public RgbColor Subtle { get; } = new(88, 110, 117);
	public RgbColor Accent { get; } = new(236, 120, 58);
	public RgbColor Banner { get; } = new(230, 106, 44);
}
