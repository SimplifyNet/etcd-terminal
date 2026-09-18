using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Localization;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Localization;
using EtcdTerminal.Security;
using EtcdTerminal.Session;
using EtcdTerminal.Terminal;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class StatusBarPersistenceTests
{
	[SetUp]
	public void SetUp()
	{
		LocalizationStore.Current = new EnglishLocalization();
		AppSettingsStore.Current = new AppSettings { PageSize = 30, TrimInputValues = true };
	}

	[Test]
	public void PromptAsk_RendersStatusBarWithActiveConnection()
	{
		var terminal = new FakeTerminal();
		var session = new ConnectionSession();

		session.Start(new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://localhost:2379" }, UserCapabilities.Unrestricted);

		var prompt = new Prompt(terminal, terminal, terminal, new StubTextInput("value"), new StatusBar(terminal, terminal, terminal, new StubAppInfo(), session));

		var result = prompt.Ask("Enter:");

		Assert.That(result, Is.EqualTo("value"));
		Assert.That(terminal.Output.ToString(), Does.Contain("prod"));
	}

	[Test]
	public void PressAnyKey_RendersStatusBarWithActiveConnection()
	{
		var terminal = new FakeTerminal();
		var session = new ConnectionSession();

		session.Start(new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://localhost:2379" }, UserCapabilities.Unrestricted);
		terminal.Press(ConsoleKey.Enter);

		new PressAnyKeyPrompt(terminal, terminal, new StatusBar(terminal, terminal, terminal, new StubAppInfo(), session)).Show();

		Assert.That(terminal.Output.ToString(), Does.Contain("prod"));
	}

	[Test]
	public void PressAnyKey_ScrollsContentSoLabelStaysAboveStatusBar()
	{
		var terminal = new FakeTerminal { CursorTop = 38 };
		var session = new ConnectionSession();

		session.Start(new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://localhost:2379" }, UserCapabilities.Unrestricted);
		terminal.Press(ConsoleKey.Enter);

		new PressAnyKeyPrompt(terminal, terminal, new StatusBar(terminal, terminal, terminal, new StubAppInfo(), session)).Show();

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
