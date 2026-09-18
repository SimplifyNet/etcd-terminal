using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Localization;
using EtcdTerminal.Session;
using EtcdTerminal.Environment;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class MenuTests
{
	[SetUp]
	public void SetUp() => LocalizationStore.Current = new EnglishLocalization();

	[Test]
	public void Show_WithDuplicateLabels_ReturnsChosenItem()
	{
		var terminal = new FakeTerminal();
		var menu = CreateMenu(terminal);

		IReadOnlyList<MenuItem<int>> items = [new(1, "same"), new(2, "same")];

		terminal.Press(ConsoleKey.DownArrow, ConsoleKey.Enter);

		var chosen = menu.Show("title", items);

		Assert.That(chosen?.Id, Is.EqualTo(2));
	}

	[Test]
	public void Show_LabelWithBrackets_RendersUnchanged()
	{
		var terminal = new FakeTerminal();
		var menu = CreateMenu(terminal);

		IReadOnlyList<MenuItem<int>> items = [new(1, "http://[::1]:2379")];

		terminal.Press(ConsoleKey.Enter);

		menu.Show("title", items);

		Assert.That(terminal.Output.ToString(), Does.Contain("[::1]"));
	}

	[Test]
	public void Show_SkipsNonSelectableItem_WhenNavigating()
	{
		var terminal = new FakeTerminal();
		var menu = CreateMenu(terminal);

		IReadOnlyList<MenuItem<int>> items = [new(1, "a"), new(0, string.Empty, IsSelectable: false), new(2, "b")];

		terminal.Press(ConsoleKey.DownArrow, ConsoleKey.Enter);

		var chosen = menu.Show("title", items);

		Assert.That(chosen?.Id, Is.EqualTo(2));
	}

	private static Menu CreateMenu(FakeTerminal terminal) => new(terminal, new StatusBar(terminal, terminal, terminal, new StubAppInfo(), new ConnectionSession()));

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
