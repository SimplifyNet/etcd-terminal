namespace EtcdTerminal.Presentation.Theming;

public interface ITheme
{
	string Id { get; }
	string Name { get; }
	string Variant { get; }

	RgbColor WindowBackground { get; }
	RgbColor BandBackground { get; }
	RgbColor ActionPanelTitleBackground { get; }
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