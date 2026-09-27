using EtcdTerminal.Presentation;

namespace EtcdTerminal.Tests.Fakes;

/// <summary>
/// Captures panel models instead of drawing them, so that model content and
/// roles can be asserted without depending on terminal geometry.
/// </summary>
public sealed class FakePanelRenderer : IPanelRenderer
{
	public List<PanelModel> Models { get; } = [];

	public void Write(PanelModel panel) => Models.Add(panel);

	public PanelModel Last => Models[^1];
}
