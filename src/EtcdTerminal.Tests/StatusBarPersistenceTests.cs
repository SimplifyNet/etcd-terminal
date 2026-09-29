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
	public void PressAnyKey_FrameCarriesStatusBarWithActiveConnection()
	{
		var terminal = new FakeTerminal();
		var session = new ConnectionSession();
		var localization = new EnglishLocalization();
		var host = new FakeScreenHost();
		var statusBar = new StatusBar(terminal, terminal, new StubAppInfo(), session, localization, new FakeStatusBarRenderer());

		session.Start(new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://localhost:2379" }, UserCapabilities.Unrestricted);
		terminal.Press(ConsoleKey.Enter);

		new PressAnyKeyPrompt(host, new Header(), statusBar, terminal, localization).Show([]);

		Assert.That(host.Current.Footer?.Name?.Text, Is.EqualTo("prod"));
		Assert.That(host.Current.Footer?.Hints, Has.Count.EqualTo(1));
		Assert.That(host.Current.Footer?.Hints.Single().Text, Is.EqualTo(localization.PressAnyKey));
		Assert.That(host.Current.Body, Is.Empty);
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
