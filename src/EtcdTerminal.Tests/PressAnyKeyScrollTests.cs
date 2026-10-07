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
public sealed class PressAnyKeyScrollTests
{
	[Test]
	public void PressAnyKey_ScrollKeysScrollThePageInsteadOfContinuing()
	{
		var canvas = new FakeScreenCanvas();
		var keys = new FakeKeyReader();

		keys.Press(ConsoleKey.DownArrow, ConsoleKey.PageDown, ConsoleKey.UpArrow, ConsoleKey.Home, ConsoleKey.End, ConsoleKey.Enter);

		new PressAnyKeyPrompt(Screen(canvas), keys, new EnglishLocalization()).Show([]);

		Assert.Multiple(() =>
		{
			Assert.That(canvas.Scrolls, Is.EqualTo(new[]
			{
				ScrollStep.LineDown,
				ScrollStep.PageDown,
				ScrollStep.LineUp,
				ScrollStep.Top,
				ScrollStep.Bottom
			}), "every scroll key moves the page and leaves the screen open");
			Assert.That(keys.KeyAvailable, Is.False, "only the key after the scroll keys continued the screen");
		});
	}

	[Test]
	public void PressAnyKey_ScrollKeysAreForwardedEvenWhenThePageDoesNotMove()
	{
		var canvas = new FakeScreenCanvas();
		var keys = new FakeKeyReader();

		keys.Press(ConsoleKey.DownArrow, ConsoleKey.Enter);

		new PressAnyKeyPrompt(Screen(canvas), keys, new EnglishLocalization()).Show([]);

		Assert.Multiple(() =>
		{
			Assert.That(canvas.Scrolls, Is.EqualTo(new[] { ScrollStep.LineDown }));
			Assert.That(keys.KeyAvailable, Is.False);
		});
	}

	[Test]
	public void Message_ScrollKeysAreSwallowedWithoutScrolling()
	{
		var canvas = new FakeScreenCanvas();
		var keys = new FakeKeyReader();
		var session = new ConnectionSession();

		session.Start(new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://localhost:2379" }, UserCapabilities.Unrestricted);

		keys.Press(ConsoleKey.DownArrow, ConsoleKey.PageUp, ConsoleKey.Enter);

		new Message(Screen(canvas, session), keys, new EnglishLocalization()).ShowSuccess("Done");

		Assert.Multiple(() =>
		{
			Assert.That(canvas.Scrolls, Is.Empty, "a message did not open a page, so a wheel must not redraw one");
			Assert.That(keys.KeyAvailable, Is.False);
		});
	}

	private static Screen Screen(FakeScreenCanvas canvas, ConnectionSession? session = null)
	{
		session ??= ConnectedSession();

		return new Screen(canvas, new Header(), new StatusBar(new StubAppInfo(), session, new EnglishLocalization()));
	}

	private static ConnectionSession ConnectedSession()
	{
		var session = new ConnectionSession();

		session.Start(new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://localhost:2379" }, UserCapabilities.Unrestricted);

		return session;
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
