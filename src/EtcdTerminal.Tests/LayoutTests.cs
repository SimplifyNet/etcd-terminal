using EtcdTerminal.App.Components;
using EtcdTerminal.App.Localization;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Keys;
using EtcdTerminal.Security;
using EtcdTerminal.Session;
using EtcdTerminal.Terminal;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class LayoutTests
{
	[Test]
	public void LongConnectionDetails_FooterModelKeepsFullLiteralValues()
	{
		var harness = new Harness(40, 24);

		harness.Session.Start(new EtcdConnectionConfig
		{
			Name = new string('n', 60),
			ConnectionString = "http://" + new string('h', 90) + ":2379",
			Username = new string('u', 40)
		}, UserCapabilities.Unrestricted);

		harness.StatusBar.Render();

		var model = harness.StatusBarRenderer.Last;

		Assert.That(model.Name?.Text, Is.EqualTo(new string('n', 60)));
		Assert.That(model.Connection?.Text, Is.EqualTo("http://" + new string('h', 90) + ":2379"));
		Assert.That(model.Username?.Text, Is.EqualTo(new string('u', 40)));
	}

	[Test]
	public void ShortTerminal_NoNegativePositions()
	{
		var harness = new Harness(80, 2);

		harness.StatusBar.Render();
		harness.StatusBar.EnsureCursorAboveBar();

		foreach (var (left, top) in harness.Terminal.CursorSets)
		{
			Assert.That(left, Is.GreaterThanOrEqualTo(0));
			Assert.That(top, Is.GreaterThanOrEqualTo(0));
		}
	}

	[Test]
	public void Shell_ComposesBannerAndFooterAndReleasesTheHost()
	{
		var harness = new Harness(80, 24);

		harness.Shell.Show();

		Assert.That(harness.Host.BeginCount, Is.EqualTo(1));
		Assert.That(harness.Host.EndCount, Is.EqualTo(1));
		Assert.That(harness.Host.IsRunning, Is.False);
		Assert.That(harness.Host.Current.Header, Is.Not.Null);
		Assert.That(harness.Host.Current.Footer, Is.Not.Null);
		Assert.That(harness.Host.Current.Body, Is.Empty);
	}

	private sealed class Harness
	{
		public readonly RecordingTerminal Terminal;
		public readonly ConnectionSession Session = new();
		public readonly FakeStatusBarRenderer StatusBarRenderer = new();
		public readonly StatusBar StatusBar;
		public readonly FakeScreenHost Host = new();
		public readonly ScreenShell Shell;

		public Harness(int width, int height)
		{
			Terminal = new RecordingTerminal { WindowWidth = width, WindowHeight = height };

			var localization = new EnglishLocalization();

			StatusBar = new StatusBar(Terminal, Terminal, new StubAppInfo(), Session, localization, StatusBarRenderer);
			Shell = new ScreenShell(Host, new Header(), StatusBar);
		}
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
