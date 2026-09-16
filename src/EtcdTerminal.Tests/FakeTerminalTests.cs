using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class FakeTerminalTests
{
	[Test]
	public void WriteLine_AppendsText()
	{
		var terminal = new FakeTerminal();

		terminal.WriteLine("hello");

		Assert.That(terminal.Output.ToString(), Does.Contain("hello"));
	}
}
