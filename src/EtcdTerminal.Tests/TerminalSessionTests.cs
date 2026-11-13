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
		var themes = Themes();
		var session = new ConsoleTerminalSession(console, themes);

		session.Start();

		Assert.That(console.Output, Does.Contain("\u001b[1;21r"));
	}

	[Test]
	public void Stop_ResetsTheScrollRegion()
	{
		var console = new TestConsole();
		var themes = Themes();
		var session = new ConsoleTerminalSession(console, themes);

		session.Stop();

		Assert.That(console.Output, Does.Contain("\u001b[r"));
	}

	[Test]
	public void Copy_WritesAnOsc52ClipboardSequenceForTheText()
	{
		var console = new TestConsole();
		var session = new ConsoleTerminalSession(console, Themes());

		var copied = session.Copy("hello");

		Assert.Multiple(() =>
		{
			Assert.That(copied, Is.True);
			Assert.That(console.Output, Does.Contain("\u001b]52;c;aGVsbG8=\u0007"));
		});
	}

	[Test]
	public void ThemeChange_UpdatesTheConsoleBackgroundAndRedrawsTheScreen()
	{
		var console = new TestConsole();
		var themes = Themes();
		var session = new ConsoleTerminalSession(console, themes);
		var redraws = 0;

		session.OnResize(() => redraws++);
		session.Start();

		var outputBeforeChange = console.Output.Length;

		themes.Set("EtcdBlue");

		Assert.Multiple(() =>
		{
			Assert.That(console.Output[outputBeforeChange..], Does.Contain("\u001b]11;#051223\u0007"));
			Assert.That(redraws, Is.EqualTo(1));
		});

		session.Stop();
	}

	private static ThemeCatalog Themes() => new();
}
