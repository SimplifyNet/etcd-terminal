using EtcdTerminal.App.Components;
using EtcdTerminal.App.Localization;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Presentation;
using EtcdTerminal.Security;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class CancellableLoadTests
{
	[Test]
	public async Task RunAsync_CompletedLoad_ReturnsTheValueWithoutWarning()
	{
		var harness = new Harness();

		var loaded = await harness.Load.RunAsync("loading", _ => Task.FromResult("value"));

		Assert.Multiple(() =>
		{
			Assert.That(loaded, Is.EqualTo("value"));
			Assert.That(harness.Canvas.Blocks, Is.Empty, "a completed load warns about nothing");
		});
	}

	[Test]
	public async Task RunAsync_EscapePressed_ReturnsNullAndWarnsThatItWasCancelled()
	{
		var harness = new Harness();

		harness.Keys.Press(ConsoleKey.Escape, ConsoleKey.Enter);

		var loaded = await harness.Load.RunAsync("loading", async ct =>
		{
			await Task.Delay(Timeout.Infinite, ct);

			return "never";
		});

		Assert.Multiple(() =>
		{
			Assert.That(loaded, Is.Null);
			Assert.That(CanvasText(harness), Does.Contain("Operation cancelled."));
		});
	}

	[Test]
	public void RunAsync_ThrowingLoad_PropagatesTheFailure()
	{
		var harness = new Harness();

		Assert.ThrowsAsync<InvalidOperationException>(async () =>
			await harness.Load.RunAsync("loading", _ => Task.FromException<string>(new InvalidOperationException("boom"))));
	}

	private static string CanvasText(Harness harness) =>
		string.Join('\n', harness.Canvas.Blocks.OfType<TextBlock>().SelectMany(block => block.Lines.Select(LineText.Of)));

	private sealed class Harness
	{
		public readonly FakeScreenCanvas Canvas = new();
		public readonly FakeKeyReader Keys = new();
		public readonly CancellableLoad Load;

		public Harness()
		{
			LocalizationCatalog localization = new();
			var screen = new Screen(Canvas, new Header(), new StatusBar(new StubAppInfo(), ConnectedSession(), localization));
			var spinner = new Spinner(Keys, new FakeStatusIndicator());

			Load = new CancellableLoad(spinner, new Message(screen, Keys, localization), localization);
		}

		private static ConnectionSession ConnectedSession()
		{
			var session = new ConnectionSession();

			session.Start(new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://localhost:2379" }, UserCapabilities.Unrestricted);

			return session;
		}
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
