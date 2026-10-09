using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming.Themes;

public sealed class RosePineDawnTheme : ITheme
{
	public string Id => "RosePineDawn";
	public string Name => "Rosé Pine";
	public string Variant => "Light";

	public RgbColor WindowBackground { get; } = new(250, 244, 237);
	public RgbColor BandBackground { get; } = new(242, 233, 222);
	public RgbColor ActionPanelTitleBackground { get; } = new(245, 237, 228);
	public RgbColor Primary { get; } = new(87, 82, 121);
	public RgbColor Secondary { get; } = new(40, 105, 131);
	public RgbColor Success { get; } = new(45, 115, 72);
	public RgbColor Danger { get; } = new(155, 72, 95);
	public RgbColor Warning { get; } = new(138, 92, 18);
	public RgbColor Muted { get; } = new(103, 98, 135);
	public RgbColor Subtle { get; } = new(93, 88, 125);
	public RgbColor Accent { get; } = new(180, 99, 122);
	public RgbColor Banner { get; } = new(190, 105, 100);
}
