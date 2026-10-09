using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class UbuntuTheme : ITheme
{
	public string Id => "Ubuntu";
	public string Name => "Ubuntu";
	public string Variant => "Dark";

	public RgbColor WindowBackground { get; } = new(48, 10, 36);
	public RgbColor BandBackground { get; } = new(61, 26, 48);
	public RgbColor ActionPanelTitleBackground { get; } = new(55, 18, 42);
	public RgbColor Primary { get; } = new(238, 238, 236);
	public RgbColor Secondary { get; } = new(84, 204, 210);
	public RgbColor Success { get; } = new(114, 196, 72);
	public RgbColor Danger { get; } = new(235, 75, 65);
	public RgbColor Warning { get; } = new(237, 196, 48);
	public RgbColor Muted { get; } = new(168, 160, 166);
	public RgbColor Subtle { get; } = new(128, 118, 126);
	public RgbColor Accent { get; } = new(233, 84, 32);
	public RgbColor Banner { get; } = new(255, 119, 64);
}
