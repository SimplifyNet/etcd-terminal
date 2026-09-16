using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Terminal;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class MenuTests
{
	[Test]
	public void Show_WithDuplicateLabels_ReturnsChosenItem()
	{
		var terminal = new FakeTerminal();
		var menu = new Menu(terminal, new StatusBar(terminal));

		IReadOnlyList<MenuItem<int>> items = [new(1, "same"), new(2, "same")];

		terminal.Press(ConsoleKey.DownArrow, ConsoleKey.Enter);

		var chosen = menu.Show("title", items);

		Assert.That(chosen?.Id, Is.EqualTo(2));
	}

	[Test]
	public void Show_LabelWithBrackets_RendersUnchanged()
	{
		var terminal = new FakeTerminal();
		var menu = new Menu(terminal, new StatusBar(terminal));

		IReadOnlyList<MenuItem<int>> items = [new(1, "http://[::1]:2379")];

		terminal.Press(ConsoleKey.Enter);

		menu.Show("title", items);

		Assert.That(terminal.Output.ToString(), Does.Contain("[::1]"));
	}

	[Test]
	public void Show_SkipsNonSelectableItem_WhenNavigating()
	{
		var terminal = new FakeTerminal();
		var menu = new Menu(terminal, new StatusBar(terminal));

		IReadOnlyList<MenuItem<int>> items = [new(1, "a"), new(0, string.Empty, IsSelectable: false), new(2, "b")];

		terminal.Press(ConsoleKey.DownArrow, ConsoleKey.Enter);

		var chosen = menu.Show("title", items);

		Assert.That(chosen?.Id, Is.EqualTo(2));
	}
}
