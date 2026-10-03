using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Theming;
using EtcdTerminal.Environment;
using EtcdTerminal.Infrastructure.Terminal;
using EtcdTerminal.Presentation.Terminal;
using EtcdTerminal.Presentation;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;
using Spectre.Console.Testing;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ScreenFrameTests
{
	[Test]
	public void FramedMenu_ComposesBannerItemsAndFooterInOneFrame()
	{
		var theme = new ReddyTheme();
		var console = new TestConsole();
		var terminal = new FakeTerminal();
		var statusBar = new StatusBar(new StubAppInfo(), new ConnectionSession(), new EnglishLocalization());
		var header = new Header();

		var menu = new Menu(terminal, terminal, Host(console, theme), header, statusBar);

		terminal.Press(ConsoleKey.DownArrow);
		terminal.Press(ConsoleKey.Escape);

		var items = new[]
		{
			new MenuItem<int>(1, "prod", true),
			new MenuItem<int>(2, "staging", true)
		};

		menu.ShowFramed(string.Empty, items);

		Assert.That(console.Output, Does.Contain("prod"));
		Assert.That(console.Output, Does.Contain("staging"));
		Assert.That(console.Output, Does.Contain("v0.0"));
		Assert.That(
			console.Lines.Count(line => line.Trim().Length > 0),
			Is.GreaterThanOrEqualTo(8),
			"banner, menu items and footer share one frame");
	}

	[Test]
	public void FramedMenu_ReplacesTheFrameAndAlwaysEndsTheHost()
	{
		var theme = new ReddyTheme();
		var terminal = new FakeTerminal();
		var host = new FakeScreenHost();
		var statusBar = new StatusBar(new StubAppInfo(), new ConnectionSession(), new EnglishLocalization());
		var menu = new Menu(terminal, terminal, host, new Header(), statusBar);

		terminal.Press(ConsoleKey.DownArrow);
		terminal.Press(ConsoleKey.Enter);

		menu.ShowFramed(string.Empty,
		[
			new MenuItem<int>(1, "prod", true),
			new MenuItem<int>(2, "staging", true)
		]);

		Assert.Multiple(() =>
		{
			Assert.That(host.BeginCount, Is.EqualTo(1));
			Assert.That(host.EndCount, Is.EqualTo(1));
			Assert.That(host.IsRunning, Is.False);
			Assert.That(host.Frames.Count, Is.EqualTo(2), "one frame per selection change, never appended content");
		});
	}

	[Test]
	public void FramedMenu_ReleasesTheHostWhenCancelled()
	{
		var theme = new ReddyTheme();
		var terminal = new FakeTerminal();
		var host = new FakeScreenHost();
		var statusBar = new StatusBar(new StubAppInfo(), new ConnectionSession(), new EnglishLocalization());

		var header = new Header();

		var menu = new Menu(terminal, terminal, host, header, statusBar);

		terminal.Press(ConsoleKey.Escape);

		var result = menu.ShowFramed(string.Empty, [new MenuItem<int>(1, "prod", true)]);

		Assert.Multiple(() =>
		{
			Assert.That(result, Is.Null);
			Assert.That(host.EndCount, Is.EqualTo(1));
		});
	}

	[Test]
	public void FramedMenu_MovesTheMarkerAndKeepsUnavailableItems()
	{
		var terminal = new FakeTerminal();
		var host = new FakeScreenHost();
		var statusBar = new StatusBar(new StubAppInfo(), new ConnectionSession(), new EnglishLocalization());
		var menu = new Menu(terminal, terminal, host, new Header(), statusBar);

		terminal.Press(ConsoleKey.Enter);

		menu.ShowFramed(string.Empty,
		[
			new MenuItem<int>(1, "prod", true),
			new MenuItem<int>(0, string.Empty, false),
			new MenuItem<int>(2, "staging", true)
		]);

		var rows = ((TextBlock)host.Current.Body[^1]).Lines;

		Assert.Multiple(() =>
		{
			Assert.That(rows[0][0].Role, Is.EqualTo(TextRole.Accent), "Enter accepts the selected item");
			Assert.That(rows[0][0].Text, Does.Contain("\u276f"));
			Assert.That(rows[1][0].Role, Is.EqualTo(TextRole.Primary));
			Assert.That(rows[1][0].Text, Does.Not.Contain("\u276f"));
		});
	}

	private static SpectreScreenHost Host(TestConsole console, ReddyTheme theme) =>
		new(console, new BlockRenderer(new RoleStyleMapper(theme), theme), new StatusBarRenderer(console, new RoleStyleMapper(theme)), new ConsoleTerminalSession(console, theme));

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
