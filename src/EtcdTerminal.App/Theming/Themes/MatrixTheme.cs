using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class MatrixTheme : ITheme
{
	public string Id => "Matrix";
	public string Name => "Matrix";
	public string Variant => "Dark";

	public RgbColor WindowBackground { get; } = new(7, 12, 7);
	public RgbColor BandBackground { get; } = new(17, 28, 17);
	public RgbColor ActionPanelTitleBackground { get; } = new(12, 21, 12);
	public RgbColor Primary { get; } = new(214, 255, 214);
	public RgbColor Secondary { get; } = new(0, 230, 140);
	public RgbColor Success { get; } = new(120, 245, 120);
	public RgbColor Danger { get; } = new(255, 95, 95);
	public RgbColor Warning { get; } = new(240, 225, 70);
	public RgbColor Muted { get; } = new(120, 165, 120);
	public RgbColor Subtle { get; } = new(95, 140, 95);
	public RgbColor Accent { get; } = new(0, 255, 65);
	public RgbColor Banner { get; } = new(0, 255, 65);
}
