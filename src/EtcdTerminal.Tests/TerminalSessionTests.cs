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

		Assert.That(console.Output, Does.Contain("\u001b[1;21r"));
	}

	[Test]
	public void Stop_ResetsTheScrollRegion()
	{
		var console = new TestConsole();
		var session = new ConsoleTerminalSession(console, new ReddyTheme());

		session.Stop();

		Assert.That(console.Output, Does.Contain("\u001b[r"));
	}
}
