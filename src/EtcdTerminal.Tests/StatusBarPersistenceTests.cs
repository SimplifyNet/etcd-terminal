using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Localization;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Presentation;
using EtcdTerminal.Security;
using EtcdTerminal.Session;
using EtcdTerminal.Terminal;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class StatusBarPersistenceTests
{
	[Test]
	public void PromptAsk_ShowsStatusBarWithActiveConnection()
	{
		var terminal = new FakeTerminal();
		var session = new ConnectionSession();
		var footer = new FakeStatusBarRenderer();

		session.Start(new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://localhost:2379" }, UserCapabilities.Unrestricted);

		var prompt = new Prompt(terminal, terminal, terminal, new StubTextInput("value"), new StatusBar(terminal, terminal, new StubAppInfo(), session, new EnglishLocalization(), footer));

		var result = prompt.Ask("Enter:");

		Assert.That(result, Is.EqualTo("value"));
		Assert.That(footer.Models, Is.Not.Empty);
		Assert.That(footer.Last.Name?.Text, Is.EqualTo("prod"));
	}

	[Test]
	public void PressAnyKey_ShowsStatusBarWithActiveConnection()
	{
		var terminal = new FakeTerminal();
		var session = new ConnectionSession();
		var footer = new FakeStatusBarRenderer();

		session.Start(new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://localhost:2379" }, UserCapabilities.Unrestricted);
		terminal.Press(ConsoleKey.Enter);

		new PressAnyKeyPrompt(terminal, terminal, new StatusBar(terminal, terminal, new StubAppInfo(), session, new EnglishLocalization(), footer), new EnglishLocalization()).Show();

		Assert.That(footer.Models, Is.Not.Empty);
		Assert.That(footer.Last.Name?.Text, Is.EqualTo("prod"));
	}

	[Test]
	public void PressAnyKey_ScrollsContentSoLabelStaysAboveStatusBar()
	{
		var terminal = new FakeTerminal { CursorTop = 38 };
		var session = new ConnectionSession();

		session.Start(new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://localhost:2379" }, UserCapabilities.Unrestricted);
		terminal.Press(ConsoleKey.Enter);

		new PressAnyKeyPrompt(terminal, terminal, new StatusBar(terminal, terminal, new StubAppInfo(), session, new EnglishLocalization(), new FakeStatusBarRenderer()), new EnglishLocalization()).Show();

		Assert.That(terminal.CursorTop, Is.EqualTo(terminal.WindowHeight - StatusBar.ReservedRows - 2));
	}

	private sealed class StubTextInput(string answer) : ITextInput
	{
		public string? ReadLine(string prompt, string? defaultValue = null) => answer;

		public string? ReadSecret(string prompt) => answer;
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
