using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Theming;
using EtcdTerminal.Infrastructure.Terminal;
using EtcdTerminal.Presentation;
using NUnit.Framework;
using Spectre.Console;
using Spectre.Console.Rendering;
using Spectre.Console.Testing;

namespace EtcdTerminal.Tests;

/// <summary>
/// The widgets the infrastructure runs reveal the cursor when they finish,
/// while the session keeps it hidden. Every adapter has to hand the cursor
/// back hidden, or the next message blinks.
/// </summary>
[TestFixture]
public sealed class CursorVisibilityTests
{
	[Test]
	public void Select_LeavesTheCursorHidden()
	{
		var inner = new TestConsole().Interactive();
		var cursor = new RecordingCursor();

		inner.Input.PushKey(ConsoleKey.Enter);

		Prompt(new RecordingConsole(inner, cursor)).Select(new ChoiceList<int>(null, [new(1, "first")]));

		Assert.That(cursor.Visible, Is.False);
	}

	[Test]
	public async Task RunAsync_LeavesTheCursorHidden()
	{
		var inner = new TestConsole().Interactive();
		var cursor = new RecordingCursor();

		var status = new SpectreStatusIndicator(new RecordingConsole(inner, cursor), new RoleStyleMapper(new ReddyTheme()));

		await status.RunAsync(new StyledText("Working", TextRole.Muted), () => Task.CompletedTask);

		Assert.That(cursor.Visible, Is.False);
	}

	private static SpectreSelectionPrompt Prompt(IAnsiConsole console) =>
		new(console, new RoleStyleMapper(new ReddyTheme()), new EnglishLocalization());

	private sealed class RecordingConsole(TestConsole _inner, RecordingCursor _cursor) : IAnsiConsole
	{
		public Profile Profile => _inner.Profile;

		public IAnsiConsoleCursor Cursor => _cursor;

		public IAnsiConsoleInput Input => _inner.Input;

		public IExclusivityMode ExclusivityMode => _inner.ExclusivityMode;

		public RenderPipeline Pipeline => _inner.Pipeline;

		public void Clear(bool home) => _inner.Clear(home);

		public void Write(IRenderable renderable) => _inner.Write(renderable);

		public void WriteAnsi(Action<AnsiWriter> action) => _inner.WriteAnsi(action);
	}

	private sealed class RecordingCursor : IAnsiConsoleCursor
	{
		public bool Visible { get; private set; } = true;

		public void Move(CursorDirection direction, int steps)
		{
		}

		public void SetPosition(int column, int line)
		{
		}

		public void Show(bool show) => Visible = show;
	}
}
