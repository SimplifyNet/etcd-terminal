namespace EtcdTerminal.Theming;

public interface ITheme
{
	string Name { get; }

	RgbColor WindowBackground { get; }
	RgbColor PanelBackground { get; }
	RgbColor PanelDarkerBackground { get; }
	RgbColor White { get; }
	RgbColor Grey { get; }
	RgbColor Green { get; }
	RgbColor Teal { get; }
	RgbColor Yellow { get; }
	RgbColor Dim { get; }
	RgbColor Accent { get; }
}