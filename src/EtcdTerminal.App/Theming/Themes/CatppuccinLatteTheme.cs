using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class CatppuccinLatteTheme : ITheme
{
	public string Id => "CatppuccinLatte";
	public string Name => "Catppuccin";
	public string Variant => "Light";

	public RgbColor WindowBackground { get; } = new(239, 241, 245);
	public RgbColor BandBackground { get; } = new(220, 224, 232);
	public RgbColor ActionPanelTitleBackground { get; } = new(230, 233, 239);
	public RgbColor Primary { get; } = new(76, 79, 105);
	public RgbColor Secondary { get; } = new(21, 88, 196);
	public RgbColor Success { get; } = new(40, 110, 30);
	public RgbColor Danger { get; } = new(190, 10, 48);
	public RgbColor Warning { get; } = new(140, 88, 0);
	public RgbColor Muted { get; } = new(95, 98, 118);
	public RgbColor Subtle { get; } = new(82, 85, 104);
	public RgbColor Accent { get; } = new(136, 57, 239);
	public RgbColor Banner { get; } = new(136, 57, 239);
}
