using EtcdTerminal.Presentation;

namespace EtcdTerminal.Tests.Fakes;

/// <summary>
/// Runs the interaction immediately and records every frame the component
/// composed instead of drawing it, so tests can assert content and update
/// counts without terminal geometry.
/// </summary>
public sealed class FakeLiveFrame : ILiveFrame
{
	public List<FrameModel> Frames { get; } = [];

	public List<LiveFrameEnd> Ends { get; } = [];

	public T Run<T>(FrameModel initial, LiveFrameEnd end, Func<ILiveFrameUpdater, T> interaction)
	{
		Frames.Add(initial);
		Ends.Add(end);

		return interaction(new Updater(Frames));
	}

	private sealed class Updater(List<FrameModel> _frames) : ILiveFrameUpdater
	{
		public void Update(FrameModel model) => _frames.Add(model);
	}
}
