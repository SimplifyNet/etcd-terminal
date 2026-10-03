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
		var keys = new FakeKeyReader();
		var status = new FakeStatusIndicator();
		var spinner = new Spinner(keys, status);

		var completed = await spinner.RunAsync("loading", _ => Task.CompletedTask);

		Assert.That(completed, Is.True);
		Assert.That(status.Messages.Single().Text, Is.EqualTo("loading"));
	}

	[Test]
	public async Task RunAsync_EscapePressed_ReturnsFalse()
	{
		var keys = new FakeKeyReader();
		var spinner = new Spinner(keys, new FakeStatusIndicator());

		keys.Press(ConsoleKey.Escape);

		var completed = await spinner.RunAsync("loading", ct => Task.Delay(Timeout.Infinite, ct));

		Assert.That(completed, Is.False);
	}

	[Test]
	public void RunAsync_FailingAction_PropagatesTheFailure()
	{
		var spinner = new Spinner(new FakeKeyReader(), new FakeStatusIndicator());

		Assert.ThrowsAsync<InvalidOperationException>(async () =>
			await spinner.RunAsync("loading", _ => throw new InvalidOperationException("boom")));
	}
}
