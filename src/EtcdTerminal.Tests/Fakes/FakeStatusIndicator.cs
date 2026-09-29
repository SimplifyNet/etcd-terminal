using EtcdTerminal.Presentation;

namespace EtcdTerminal.Tests.Fakes;

/// <summary>
/// Runs the indicator body immediately instead of animating it. The animation
/// belongs to Infrastructure, so a behavior test only needs the indicator to
/// keep the operation alive for as long as the operation itself takes.
/// </summary>
public sealed class FakeStatusIndicator : IStatusIndicator
{
	public List<StyledText> Messages { get; } = [];

	public Task RunAsync(StyledText message, Func<Task> action)
	{
		Messages.Add(message);

		return action();
	}
}
