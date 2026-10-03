using System.Text;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Localization;
using EtcdTerminal.Environment;
using EtcdTerminal.Presentation;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class MultiLinePasteReaderTests
{
	[Test]
	public void CountLines_IgnoresBlankLines()
	{
		var buffer = new StringBuilder("{\n\n\"a\": 1\n}\n");

		Assert.That(MultiLinePasteReader.CountLines(buffer), Is.EqualTo(3));
	}

	[Test]
	public async Task QueuedEscape_ReturnsNullAndKeepsTheWaitingStatus()
	{
		var keys = new FakeKeyReader();

		keys.Press(ConsoleKey.Escape);

		var harness = Create(keys);

		Assert.That(await harness.Reader.ReadAsync("Paste:"), Is.Null);
		Assert.That(harness.Live.Ends, Is.EqualTo(new[] { LiveFrameEnd.Keep }));
		Assert.That(harness.Live.Frames, Has.Count.EqualTo(1));
		Assert.That(Status(harness.Live.Frames[0]), Is.EqualTo("waiting for paste..."));
		Assert.That(harness.Live.StatusRoles[0], Is.EqualTo(TextRole.Subtle));
	}

	[Test]
	public async Task QueuedContentFollowedByEscape_ReturnsNullWithoutThrowing()
	{
		var keys = new FakeKeyReader();

		foreach (var c in "{\"a\":1}")
			keys.Keys.Enqueue(new ConsoleKeyInfo(c, ConsoleKey.None, false, false, false));

		keys.Press(ConsoleKey.Escape);

		var harness = Create(keys, oneUpdatePerKey: true);

		Assert.That(await harness.Reader.ReadAsync("Paste:"), Is.Null);
		Assert.That(harness.Live.Frames, Has.Count.EqualTo(8), "the counter updates after every queued character");
		Assert.That(Status(harness.Live.Frames[^1]), Is.EqualTo("[pasted 1 lines]"));
		Assert.That(harness.Live.StatusRoles[^1], Is.EqualTo(TextRole.Accent));
	}

	[Test]
	public async Task ReadAsync_WritesThePromptAndABlankLineBeforeTheStatus()
	{
		var keys = new FakeKeyReader();

		keys.Press(ConsoleKey.Escape);

		var harness = Create(keys);

		await harness.Reader.ReadAsync("Paste JSON:");

		Assert.That(harness.Canvas.Blocks, Has.Count.EqualTo(2));
		Assert.That(LineText.Of(((TextBlock)harness.Canvas.Blocks[0]).Lines.Single()), Is.EqualTo("Paste JSON:"));
		Assert.That(LineText.Of(((TextBlock)harness.Canvas.Blocks[1]).Lines.Single()), Is.EqualTo(string.Empty));
	}

	private static Harness Create(FakeKeyReader reader, bool oneUpdatePerKey = false)
	{
		var localization = new EnglishLocalization();
		var live = new RecordingLiveFrame();
		var canvas = new FakeScreenCanvas();
		var screen = new Screen(canvas, new Header(), new StatusBar(new StubAppInfo(), new ConnectionSession(), localization));
		IKeyReader keys = oneUpdatePerKey ? new SingleStepKeyReader(reader.Keys) : reader;

		return new Harness(new MultiLinePasteReader(keys, live, screen, localization), live, canvas);
	}

	private static string Status(FrameModel frame) =>
		LineText.Of(((TextBlock)frame.Body.Single()).Lines.Single());

	private sealed record Harness(MultiLinePasteReader Reader, RecordingLiveFrame Live, FakeScreenCanvas Canvas);

	/// Pretends no further input is queued, so the component repaints the
	/// counter after every key instead of waiting for the burst to end.
	private sealed class SingleStepKeyReader(Queue<ConsoleKeyInfo> keys) : IKeyReader
	{
		public bool KeyAvailable => false;

		public ConsoleKeyInfo ReadKey() => keys.Dequeue();
	}

	private sealed class RecordingLiveFrame : ILiveFrame
	{
		public List<FrameModel> Frames { get; } = [];

		public List<LiveFrameEnd> Ends { get; } = [];

		public List<TextRole> StatusRoles { get; } = [];

		public T Run<T>(FrameModel initial, LiveFrameEnd end, Func<ILiveFrameUpdater, T> interaction)
		{
			Observe(initial);
			Ends.Add(end);

			return interaction(new Updater(this));
		}

		private void Observe(FrameModel model)
		{
			Frames.Add(model);
			StatusRoles.Add(((TextBlock)model.Body.Single()).Lines.Single()[^1].Role);
		}

		private sealed class Updater(RecordingLiveFrame _owner) : ILiveFrameUpdater
		{
			public void Update(FrameModel model) => _owner.Observe(model);
		}
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
