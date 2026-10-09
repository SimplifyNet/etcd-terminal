using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class VsCodeLightTheme : ITheme
{
	public string Id => "VsCodeLight";
	public string Name => "VS Code";
	public string Variant => "Light";

	public RgbColor WindowBackground { get; } = new(255, 255, 255);
	public RgbColor BandBackground { get; } = new(243, 243, 243);
	public RgbColor ActionPanelTitleBackground { get; } = new(238, 238, 238);
	public RgbColor Primary { get; } = new(51, 51, 51);
	public RgbColor Secondary { get; } = new(0, 102, 160);
	public RgbColor Success { get; } = new(22, 120, 68);
	public RgbColor Danger { get; } = new(190, 45, 45);
	public RgbColor Warning { get; } = new(140, 95, 0);
	public RgbColor Muted { get; } = new(107, 107, 107);
	public RgbColor Subtle { get; } = new(85, 85, 85);
	public RgbColor Accent { get; } = new(0, 95, 184);
	public RgbColor Banner { get; } = new(0, 122, 204);
}
