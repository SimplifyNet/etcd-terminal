namespace EtcdTerminal.Theming;

public interface ITheme
{
	string Name { get; }

	RgbColor WindowBackground { get; }
	RgbColor PanelBackground { get; }
	RgbColor PanelDarkerBackground { get; }
	RgbColor Primary { get; }
	RgbColor Secondary { get; }
	RgbColor Success { get; }
	RgbColor Danger { get; }
	RgbColor Warning { get; }
	RgbColor Muted { get; }
	RgbColor Subtle { get; }
	RgbColor Accent { get; }
	RgbColor Banner { get; }
}