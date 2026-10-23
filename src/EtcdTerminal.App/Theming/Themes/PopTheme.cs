using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class PopTheme : ITheme
{
	public string Id => "Pop";
	public string Name => "Pop! OS";
	public string Variant => "Dark";

	public RgbColor WindowBackground { get; } = new(27, 27, 27);
	public RgbColor BandBackground { get; } = new(38, 38, 38);
	public RgbColor ActionPanelTitleBackground { get; } = new(46, 46, 46);
	public RgbColor Primary { get; } = new(222, 222, 222);
	public RgbColor Secondary { get; } = new(99, 208, 223);
	public RgbColor Success { get; } = new(94, 219, 140);
	public RgbColor Danger { get; } = new(255, 160, 154);
	public RgbColor Warning { get; } = new(247, 224, 98);
	public RgbColor Muted { get; } = new(158, 158, 158);
	public RgbColor Subtle { get; } = new(128, 128, 128);
	public RgbColor Accent { get; } = new(72, 185, 199);
	public RgbColor Banner { get; } = new(99, 208, 223);
}
