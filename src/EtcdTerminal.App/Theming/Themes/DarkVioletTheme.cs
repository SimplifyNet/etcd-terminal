using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class DarkVioletTheme : ITheme
{
	public string Id => "DarkViolet";
	public string Name => "Dark Violet";
	public string Variant => "Dark";

	public RgbColor WindowBackground { get; } = new(28, 21, 40);
	public RgbColor BandBackground { get; } = new(40, 31, 57);
	public RgbColor ActionPanelTitleBackground { get; } = new(52, 37, 74);
	public RgbColor Primary { get; } = new(240, 236, 248);
	public RgbColor Secondary { get; } = new(165, 130, 220);
	public RgbColor Success { get; } = new(110, 205, 140);
	public RgbColor Danger { get; } = new(240, 105, 115);
	public RgbColor Warning { get; } = new(245, 195, 80);
	public RgbColor Muted { get; } = new(150, 142, 165);
	public RgbColor Subtle { get; } = new(108, 100, 122);
	public RgbColor Accent { get; } = new(168, 127, 232);
	public RgbColor Banner { get; } = new(154, 106, 223);
}
