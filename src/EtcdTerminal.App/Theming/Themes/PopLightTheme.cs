using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class PopLightTheme : ITheme
{
	public string Id => "PopLight";
	public string Name => "Pop! OS";
	public string Variant => "Light";

	public RgbColor WindowBackground { get; } = new(255, 255, 255);
	public RgbColor BandBackground { get; } = new(243, 246, 248);
	public RgbColor ActionPanelTitleBackground { get; } = new(234, 239, 242);
	public RgbColor Primary { get; } = new(32, 37, 42);
	public RgbColor Secondary { get; } = new(0, 105, 180);
	public RgbColor Success { get; } = new(20, 115, 62);
	public RgbColor Danger { get; } = new(196, 44, 44);
	public RgbColor Warning { get; } = new(138, 96, 0);
	public RgbColor Muted { get; } = new(96, 106, 116);
	public RgbColor Subtle { get; } = new(84, 94, 104);
	public RgbColor Accent { get; } = new(26, 118, 131);
	public RgbColor Banner { get; } = new(0, 140, 160);
}
