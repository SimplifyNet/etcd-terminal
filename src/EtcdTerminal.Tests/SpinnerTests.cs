using EtcdTerminal.App.Components;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class SpinnerTests
{
	[Test]
	public async Task RunAsync_CompletedAction_ReturnsTrue()
	{
		var terminal = new FakeTerminal();
		var spinner = new Spinner(terminal);

		var completed = await spinner.RunAsync("loading", _ => Task.CompletedTask);

		Assert.That(completed, Is.True);
	}

	[Test]
	public async Task RunAsync_EscapePressed_ReturnsFalse()
	{
		var terminal = new FakeTerminal();
		var spinner = new Spinner(terminal);

		terminal.Press(ConsoleKey.Escape);

		var completed = await spinner.RunAsync("loading", ct => Task.Delay(Timeout.Infinite, ct));

		Assert.That(completed, Is.False);
	}
}
