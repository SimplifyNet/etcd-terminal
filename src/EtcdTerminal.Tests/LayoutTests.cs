using EtcdTerminal.App.Components;
using EtcdTerminal.App.Localization;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Security;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class LayoutTests
{
	[Test]
	public void LongConnectionDetails_FooterModelKeepsFullLiteralValues()
	{
		var harness = new Harness();

		harness.Session.Start(new EtcdConnectionConfig
		{
			Name = new string('n', 60),
			ConnectionString = "http://" + new string('h', 90) + ":2379",
			Username = new string('u', 40)
		}, UserCapabilities.Unrestricted);

		var model = harness.StatusBar.BuildModel();

		Assert.That(model.Name?.Text, Is.EqualTo(new string('n', 60)));
		Assert.That(model.Connection?.Text, Is.EqualTo("http://" + new string('h', 90) + ":2379"));
		Assert.That(model.Username?.Text, Is.EqualTo(new string('u', 40)));
	}

	[Test]
	public void Screen_ComposesBannerAndFooterOnTheCanvas()
	{
		var harness = new Harness();

		harness.Screen.Open();

		Assert.Multiple(() =>
		{
			Assert.That(harness.Canvas.NewScreenCount, Is.EqualTo(1));
			Assert.That(harness.Canvas.Footers, Has.Count.EqualTo(1));
			Assert.That(harness.Canvas.Blocks, Has.Count.EqualTo(1), "an empty screen is the banner alone");
		});
	}

	private sealed class Harness
	{
		public readonly ConnectionSession Session = new();
		public readonly FakeScreenCanvas Canvas = new();
		public readonly StatusBar StatusBar;
		public readonly Screen Screen;

		public Harness()
		{
			LocalizationCatalog localization = new();

			StatusBar = new StatusBar(new StubAppInfo(), Session, localization);
			Screen = new Screen(Canvas, new Header(), StatusBar);
		}
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
