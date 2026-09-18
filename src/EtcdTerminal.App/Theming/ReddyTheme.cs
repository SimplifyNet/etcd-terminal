using EtcdTerminal.Theming;

namespace EtcdTerminal.App.Theming;

public class ReddyTheme : ITheme
{
	public string Name => "Reddy";

	public RgbColor WindowBackground { get; } = new(10, 10, 10);
	public RgbColor PanelBackground { get; } = new(27, 28, 30);
	public RgbColor PanelDarkerBackground { get; } = new(21, 22, 24);
	public RgbColor Primary { get; } = new(255, 255, 255);
	public RgbColor Secondary { get; } = new(0, 180, 180);
	public RgbColor Success { get; } = new(0, 200, 0);
	public RgbColor Danger { get; } = new(220, 50, 50);
	public RgbColor Warning { get; } = new(255, 200, 0);
	public RgbColor Muted { get; } = new(128, 128, 128);
	public RgbColor Subtle { get; } = new(80, 80, 80);
	public RgbColor Accent { get; } = new(220, 95, 51);
	public RgbColor Banner { get; } = new(255, 95, 0);
}