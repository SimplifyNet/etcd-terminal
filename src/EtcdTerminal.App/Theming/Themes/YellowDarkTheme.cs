using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class YellowDarkTheme : ITheme
{
	public string Id => "Yellow";
	public string Name => "Yellow";
	public string Variant => "Dark";

	public RgbColor WindowBackground { get; } = new(22, 19, 14);
	public RgbColor BandBackground { get; } = new(38, 33, 25);
	public RgbColor ActionPanelTitleBackground { get; } = new(30, 26, 19);
	public RgbColor Primary { get; } = new(255, 242, 214);
	public RgbColor Secondary { get; } = new(232, 189, 108);
	public RgbColor Success { get; } = new(150, 200, 100);
	public RgbColor Danger { get; } = new(230, 90, 70);
	public RgbColor Warning { get; } = new(245, 200, 80);
	public RgbColor Muted { get; } = new(160, 148, 124);
	public RgbColor Subtle { get; } = new(110, 100, 82);
	public RgbColor Accent { get; } = new(245, 167, 66);
	public RgbColor Banner { get; } = new(250, 190, 90);
}
