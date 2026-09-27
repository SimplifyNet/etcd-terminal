using System.Text;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Localization;
using EtcdTerminal.Environment;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class MultiLinePasteReaderTests
{
	[Test]
	public void CountLines_IgnoresBlankLines()
	{
		var buffer = new StringBuilder("{\n\n\"a\": 1\n}\n");

		Assert.That(MultiLinePasteReader.CountLines(buffer), Is.EqualTo(3));
	}

	[Test]
	public async Task QueuedEscape_ReturnsNull()
	{
		var terminal = new FakeTerminal();

		terminal.Press(ConsoleKey.Escape);

		Assert.That(await CreateReader(terminal).ReadAsync("Paste:"), Is.Null);
	}

	[Test]
	public async Task QueuedContentFollowedByEscape_ReturnsNullWithoutThrowing()
	{
		var terminal = new FakeTerminal();

		foreach (var c in "{\"a\":1}")
			terminal.Keys.Enqueue(new ConsoleKeyInfo(c, ConsoleKey.None, false, false, false));

		terminal.Press(ConsoleKey.Escape);

		Assert.That(await CreateReader(terminal).ReadAsync("Paste:"), Is.Null);
	}

	private static MultiLinePasteReader CreateReader(FakeTerminal terminal)
	{
		var statusBar = new StatusBar(terminal, terminal, new StubAppInfo(), new ConnectionSession(), new EnglishLocalization(), new FakeStatusBarRenderer());

		return new MultiLinePasteReader(terminal, statusBar, new EnglishLocalization());
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
