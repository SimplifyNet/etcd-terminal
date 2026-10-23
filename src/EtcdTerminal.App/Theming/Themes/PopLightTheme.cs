using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class PopLightTheme : ITheme
{
	public string Id => "PopLight";
	public string Name => "Pop! OS";
	public string Variant => "Light";

	public RgbColor WindowBackground { get; } = new(255, 255, 255);
	public RgbColor BandBackground { get; } = new(228, 228, 228);
	public RgbColor ActionPanelTitleBackground { get; } = new(237, 237, 237);
	public RgbColor Primary { get; } = new(22, 22, 22);
	public RgbColor Secondary { get; } = new(0, 82, 90);
	public RgbColor Success { get; } = new(0, 87, 44);
	public RgbColor Danger { get; } = new(137, 4, 24);
	public RgbColor Warning { get; } = new(83, 72, 0);
	public RgbColor Muted { get; } = new(72, 72, 72);
	public RgbColor Subtle { get; } = new(99, 99, 99);
	public RgbColor Accent { get; } = new(26, 118, 131);
	public RgbColor Banner { get; } = new(0, 82, 90);
}
