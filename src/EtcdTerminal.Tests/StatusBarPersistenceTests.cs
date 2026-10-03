using EtcdTerminal.App.Components;
using EtcdTerminal.App.Localization;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Presentation;
using EtcdTerminal.Security;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class StatusBarPersistenceTests
{
	[Test]
	public void ScreenOpen_CarriesStatusBarWithActiveConnection()
	{
		var session = new ConnectionSession();

		session.Start(new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://localhost:2379" }, UserCapabilities.Unrestricted);

		var canvas = new FakeScreenCanvas();

		new Screen(canvas, new Header(), new StatusBar(new StubAppInfo(), session, new EnglishLocalization())).Open();

		Assert.That(canvas.NewScreenCount, Is.EqualTo(1));
		Assert.That(canvas.Footers, Is.Not.Empty);
		Assert.That(canvas.Footers[^1].Name?.Text, Is.EqualTo("prod"));
		Assert.That(canvas.Blocks, Has.Count.EqualTo(1), "an empty screen is the banner alone");
	}

	[Test]
	public void PressAnyKey_ScreenCarriesStatusBarAndHintTogether()
	{
		var session = new ConnectionSession();
		var localization = new EnglishLocalization();
		var canvas = new FakeScreenCanvas();
		var keys = new FakeKeyReader();

		session.Start(new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://localhost:2379" }, UserCapabilities.Unrestricted);

		keys.Press(ConsoleKey.Enter);

		new PressAnyKeyPrompt(new Screen(canvas, new Header(), new StatusBar(new StubAppInfo(), session, localization)), keys, localization).Show([]);

		Assert.That(canvas.Footers[^1].Name?.Text, Is.EqualTo("prod"));
		Assert.That(LineText.Of(((TextBlock)canvas.Blocks[^1]).Lines[^1]), Is.EqualTo(localization.PressAnyKey));
		Assert.That(keys.KeyAvailable, Is.False, "the prompt waits for exactly one key");
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
