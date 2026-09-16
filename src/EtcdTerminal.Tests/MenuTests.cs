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
}
