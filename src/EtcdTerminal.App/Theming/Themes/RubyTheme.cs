using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class RubyTheme : ITheme
{
	public string Id => "Ruby";
	public string Name => "Ruby";
	public string Variant => "Dark";

	public RgbColor WindowBackground { get; } = new(30, 33, 38);
	public RgbColor BandBackground { get; } = new(49, 50, 55);
	public RgbColor ActionPanelTitleBackground { get; } = new(52, 54, 59);
	public RgbColor Primary { get; } = new(255, 255, 255);
	public RgbColor Secondary { get; } = new(255, 160, 160);
	public RgbColor Success { get; } = new(130, 210, 130);
	public RgbColor Danger { get; } = new(255, 100, 100);
	public RgbColor Warning { get; } = new(245, 195, 80);
	public RgbColor Muted { get; } = new(170, 111, 119);
	public RgbColor Subtle { get; } = new(120, 90, 95);
	public RgbColor Accent { get; } = new(239, 84, 76);
	public RgbColor Banner { get; } = new(239, 84, 76);
}
