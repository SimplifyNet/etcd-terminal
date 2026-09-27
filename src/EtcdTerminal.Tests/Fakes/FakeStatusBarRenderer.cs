using EtcdTerminal.Presentation;

namespace EtcdTerminal.Tests.Fakes;

/// <summary>
/// Captures footer models instead of drawing them. Fitting and placement belong
/// to Infrastructure, so a model test must not assert on rendered width.
/// </summary>
public sealed class FakeStatusBarRenderer : IStatusBarRenderer
{
	public List<StatusBarModel> Models { get; } = [];

	public void Write(StatusBarModel model) => Models.Add(model);

	public StatusBarModel Last => Models[^1];
}
