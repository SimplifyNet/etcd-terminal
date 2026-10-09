using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class GitHubLightTheme : ITheme
{
	public string Id => "GitHubLight";
	public string Name => "GitHub";
	public string Variant => "Light";

	public RgbColor WindowBackground { get; } = new(255, 255, 255);
	public RgbColor BandBackground { get; } = new(246, 248, 250);
	public RgbColor ActionPanelTitleBackground { get; } = new(221, 244, 255);
	public RgbColor Primary { get; } = new(31, 35, 40);
	public RgbColor Secondary { get; } = new(9, 105, 218);
	public RgbColor Success { get; } = new(25, 120, 51);
	public RgbColor Danger { get; } = new(207, 34, 46);
	public RgbColor Warning { get; } = new(143, 96, 0);
	public RgbColor Muted { get; } = new(89, 99, 110);
	public RgbColor Subtle { get; } = new(75, 85, 95);
	public RgbColor Accent { get; } = new(130, 80, 223);
	public RgbColor Banner { get; } = new(9, 105, 218);
}
