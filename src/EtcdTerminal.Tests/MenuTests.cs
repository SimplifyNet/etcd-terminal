using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Localization;
using EtcdTerminal.Environment;
using EtcdTerminal.Presentation.Terminal;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class MenuTests
{
	[Test]
	public void ShowFramed_WithDuplicateLabels_ReturnsChosenItem()
	{
		var harness = new Harness();

		IReadOnlyList<MenuItem<int>> items = [new(1, "same"), new(2, "same")];

		harness.Terminal.Press(ConsoleKey.DownArrow, ConsoleKey.Enter);

		var chosen = harness.Menu.ShowFramed("title", items);

		Assert.That(chosen?.Id, Is.EqualTo(2));
	}

	[Test]
	public void ShowFramed_LabelWithBrackets_RendersUnchanged()
	{
		var harness = new Harness();

		IReadOnlyList<MenuItem<int>> items = [new(1, "http://[::1]:2379")];

		harness.Terminal.Press(ConsoleKey.Enter);

		harness.Menu.ShowFramed("title", items);

		var frame = harness.Host.Frames.Single();

		Assert.That(frame.Body[1].Lines.Single().Text, Does.Contain("[::1]"));
	}

	[Test]
	public void ShowFramed_SkipsNonSelectableItem_WhenNavigating()
	{
		var harness = new Harness();

		IReadOnlyList<MenuItem<int>> items = [new(1, "a"), new(0, string.Empty, IsSelectable: false), new(2, "b")];

		harness.Terminal.Press(ConsoleKey.DownArrow, ConsoleKey.Enter);

		var chosen = harness.Menu.ShowFramed("title", items);

		Assert.That(chosen?.Id, Is.EqualTo(2));
	}

	[Test]
	public void ShowFramed_AlwaysReleasesTheConsole()
	{
		var harness = new Harness();

		IReadOnlyList<MenuItem<int>> items = [new(1, "a")];

		harness.Terminal.Press(ConsoleKey.Escape);

		harness.Menu.ShowFramed("title", items);

		Assert.That(harness.Host.BeginCount, Is.EqualTo(harness.Host.EndCount));
	}

	private sealed class Harness
	{
		public readonly FakeTerminal Terminal = new();
		public readonly FakeScreenHost Host = new();
		public readonly Menu Menu;

		public Harness()
		{
			var statusBar = new StatusBar(Terminal, new StubAppInfo(), new ConnectionSession(), new EnglishLocalization(), new FakeStatusBarRenderer());

			Menu = new Menu(Terminal, Terminal, Host, new Header(), statusBar);
		}
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
