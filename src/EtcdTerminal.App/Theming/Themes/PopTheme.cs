using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class PopTheme : ITheme
{
	public string Id => "Pop";
	public string Name => "Pop! OS";
	public string Variant => "Dark";

	public RgbColor WindowBackground { get; } = new(26, 30, 34);
	public RgbColor BandBackground { get; } = new(40, 45, 52);
	public RgbColor ActionPanelTitleBackground { get; } = new(33, 38, 44);
	public RgbColor Primary { get; } = new(238, 242, 246);
	public RgbColor Secondary { get; } = new(120, 190, 240);
	public RgbColor Success { get; } = new(110, 205, 130);
	public RgbColor Danger { get; } = new(240, 100, 100);
	public RgbColor Warning { get; } = new(245, 200, 80);
	public RgbColor Muted { get; } = new(145, 155, 165);
	public RgbColor Subtle { get; } = new(105, 115, 125);
	public RgbColor Accent { get; } = new(72, 185, 199);
	public RgbColor Banner { get; } = new(110, 215, 228);
}
