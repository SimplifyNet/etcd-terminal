using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class ManjaroTheme : ITheme
{
	public string Id => "Manjaro";
	public string Name => "Manjaro";
	public string Variant => "Dark";

	public RgbColor WindowBackground { get; } = new(27, 34, 36);
	public RgbColor BandBackground { get; } = new(48, 59, 62);
	public RgbColor ActionPanelTitleBackground { get; } = new(34, 43, 46);
	public RgbColor Primary { get; } = new(204, 215, 212);
	public RgbColor Secondary { get; } = new(109, 218, 196);
	public RgbColor Success { get; } = new(46, 179, 152);
	public RgbColor Danger { get; } = new(252, 65, 56);
	public RgbColor Warning { get; } = new(242, 120, 53);
	public RgbColor Muted { get; } = new(171, 185, 182);
	public RgbColor Subtle { get; } = new(107, 113, 115);
	public RgbColor Accent { get; } = new(53, 191, 164);
	public RgbColor Banner { get; } = new(53, 203, 172);
}
