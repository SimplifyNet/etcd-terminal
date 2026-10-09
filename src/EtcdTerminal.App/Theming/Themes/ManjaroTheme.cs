using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class ManjaroTheme : ITheme
{
	public string Id => "Manjaro";
	public string Name => "Manjaro";
	public string Variant => "Dark";

	public RgbColor WindowBackground { get; } = new(30, 33, 38);
	public RgbColor BandBackground { get; } = new(45, 49, 56);
	public RgbColor ActionPanelTitleBackground { get; } = new(38, 42, 48);
	public RgbColor Primary { get; } = new(235, 238, 240);
	public RgbColor Secondary { get; } = new(110, 175, 235);
	public RgbColor Success { get; } = new(96, 206, 128);
	public RgbColor Danger { get; } = new(235, 85, 85);
	public RgbColor Warning { get; } = new(243, 196, 72);
	public RgbColor Muted { get; } = new(150, 156, 162);
	public RgbColor Subtle { get; } = new(110, 116, 122);
	public RgbColor Accent { get; } = new(53, 191, 164);
	public RgbColor Banner { get; } = new(80, 214, 194);
}
