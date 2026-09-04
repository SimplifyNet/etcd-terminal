using EtcdTerminal.Theming;

namespace EtcdTerminal.App.Theming;

public class ReddyTheme : ITheme
{
	public string Name => "Reddy";

	public RgbColor WindowBackground { get; } = new(10, 10, 10);
	public RgbColor PanelBackground { get; } = new(27, 28, 30);
	public RgbColor PanelDarkerBackground { get; } = new(21, 22, 24);
	public RgbColor White { get; } = new(255, 255, 255);
	public RgbColor Grey { get; } = new(128, 128, 128);
	public RgbColor Green { get; } = new(0, 200, 0);
	public RgbColor Red { get; } = new(220, 50, 50);
	public RgbColor Teal { get; } = new(0, 180, 180);
	public RgbColor Yellow { get; } = new(255, 200, 0);
	public RgbColor Dim { get; } = new(80, 80, 80);
	public RgbColor Accent { get; } = new(220, 95, 51);
}