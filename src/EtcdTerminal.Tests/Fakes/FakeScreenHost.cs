using EtcdTerminal.Presentation;

namespace EtcdTerminal.Tests.Fakes;

/// <summary>
/// Records screen frames instead of drawing them. Lets a test assert what a
/// screen composed, and how often, without depending on terminal geometry.
/// </summary>
public sealed class FakeScreenHost : IScreenHost
{
	public List<ScreenModel> Frames { get; } = [];

	public int BeginCount { get; private set; }

	public int EndCount { get; private set; }

	public bool IsRunning => BeginCount > EndCount;

	public ScreenModel Current => Frames[^1];

	public void Begin(ScreenModel model)
	{
		BeginCount++;
		Frames.Add(model);
	}

	public void Update(ScreenModel model) => Frames.Add(model);

	public void End() => EndCount++;
}
