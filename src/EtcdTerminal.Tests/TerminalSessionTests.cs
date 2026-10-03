using EtcdTerminal.App.Theming;
using EtcdTerminal.Infrastructure.Terminal;
using EtcdTerminal.Presentation;
using NUnit.Framework;
using Spectre.Console.Testing;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class TerminalSessionTests
{
	[Test]
	public void Start_SetsTheScrollRegionAboveTheFooter()
	{
		var console = new TestConsole();
		var session = new ConsoleTerminalSession(console, new ReddyTheme());

		session.Start();

		Assert.That(console.Output, Does.Contain("\u001b[1;23r"));
	}

	[Test]
	public void BeginFrame_TakesTheWholeTerminalWhileTheFrameDrawsTheFooter()
	{
		var console = new TestConsole();
		var session = new ConsoleTerminalSession(console, new ReddyTheme());

		session.BeginFrame();

		Assert.That(console.Output, Does.Contain("\u001b[1;24r"));
	}

	[Test]
	public void EndFrame_RestoresTheViewportAboveTheFooter()
	{
		var console = new TestConsole();
		var session = new ConsoleTerminalSession(console, new ReddyTheme());

		session.EndFrame();

		Assert.That(console.Output, Does.Contain("\u001b[1;23r"));
	}

	[Test]
	public void Stop_ResetsTheScrollRegion()
	{
		var console = new TestConsole();
		var session = new ConsoleTerminalSession(console, new ReddyTheme());

		session.Stop();

		Assert.That(console.Output, Does.Contain("\u001b[r"));
	}

	[Test]
	public void ScreenHost_BracketsTheFrameWithTheScrollRegionSwitch()
	{
		var theme = new ReddyTheme();
		var console = new TestConsole();
		var session = new ConsoleTerminalSession(console, theme);
		var host = new SpectreScreenHost(
			console,
			new BlockRenderer(new RoleStyleMapper(theme), theme),
			new StatusBarRenderer(console, new RoleStyleMapper(theme)),
			session);

		host.Begin(new ScreenModel());
		host.End();

		var output = console.Output;
		var frame = output.IndexOf("\u001b[1;24r", StringComparison.Ordinal);
		var viewport = output.IndexOf("\u001b[1;23r", StringComparison.Ordinal);

		Assert.Multiple(() =>
		{
			Assert.That(frame, Is.GreaterThanOrEqualTo(0), "the frame runs on the whole terminal");
			Assert.That(viewport, Is.GreaterThan(frame), "the viewport split comes back when the frame is released");
		});
	}
}
