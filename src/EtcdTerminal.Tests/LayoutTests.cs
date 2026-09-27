using EtcdTerminal.App.Components;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens.Keys;
using EtcdTerminal.App.Screens.Roles;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Keys;
using EtcdTerminal.Roles;
using EtcdTerminal.Permissions;
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
	public void LongKeysAndValues_StayWithinWidth()
	{
		var harness = new Harness(80, 24);

		harness.Control.Render(
			[new EtcdKeyValue { Key = new string('k', 100), Value = new string('v', 200) }],
			1, 1);

		AssertBounds(harness.Terminal);
		Assert.That(harness.Terminal.Output.ToString(), Does.Contain("\u2026"));
	}

	[Test]
	public void ControlCharacters_RenderedSingleLineWithinWidth()
	{
		var harness = new Harness(80, 24);

		harness.Control.Render(
			[new EtcdKeyValue { Key = "a", Value = "x\ny\tz" }],
			1, 1);

		AssertBounds(harness.Terminal);
		Assert.That(harness.Terminal.Output.ToString(), Does.Contain("\u23CE"));
	}

	[Test]
	public void WideText_StayWithinWidth()
	{
		var harness = new Harness(80, 24);

		harness.Control.Render(
			[new EtcdKeyValue { Key = "\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d\u4e2d", Value = "\u6587\u6587\u6587\u6587\u6587\u6587\u6587\u6587\u6587\u6587\u6587\u6587\u6587\u6587\u6587\u6587\u6587\u6587\u6587\u6587\u6587\u6587\u6587\u6587\u6587" }],
			1, 1);

		AssertBounds(harness.Terminal);
		Assert.That(harness.Terminal.Output.ToString(), Does.Contain("\u2026"));
	}

	[Test]
	public void LongConnectionDetails_FooterFitsNarrowWindow()
	{
		var harness = new Harness(40, 24);

		harness.Session.Start(new EtcdConnectionConfig
		{
			Name = new string('n', 60),
			ConnectionString = "http://" + new string('h', 90) + ":2379",
			Username = new string('u', 40)
		}, UserCapabilities.Unrestricted);

		harness.StatusBar.Render();

		AssertBounds(harness.Terminal);
		Assert.That(harness.Terminal.Output.ToString(), Does.Contain("\u2026"));
	}

	[Test]
	public void VeryNarrowWindow_NoOverflowOrInvalidPositions()
	{
		var harness = new Harness(10, 24);

		harness.Session.Start(new EtcdConnectionConfig
		{
			Name = new string('n', 30),
			ConnectionString = "http://localhost:2379",
			Username = new string('u', 20)
		}, UserCapabilities.Unrestricted);

		harness.Control.Render(
			[new EtcdKeyValue { Key = new string('k', 50), Value = new string('v', 50) }],
			1, 1);

		AssertBounds(harness.Terminal);
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
	public void HeaderRender_PreservesCursorAboveFooter()
	{
		var harness = new Harness(80, 24);

		harness.Terminal.SetCursorPosition(5, 5);
		harness.Terminal.CursorSets.Clear();
		harness.Layout.RenderHeader();

		Assert.That((harness.Terminal.CursorLeft, harness.Terminal.CursorTop), Is.EqualTo((0, 5)));
		AssertBounds(harness.Terminal);
	}

	[Test]
	public void Tables_CapturedWithContents()
	{
		var harness = new Harness(80, 24);
		var roles = new[]
		{
			new EtcdRole
			{
				Name = "dev",
				Permissions = [new EtcdPermission { Type = PermissionType.Read, KeyPrefix = "/a", RangeEnd = string.Empty }]
			}
		};

		RoleListRenderer.Render(harness.Terminal, new EnglishLocalization(), roles);

		var table = harness.Terminal.Tables.Single();

		Assert.That(table.Title, Is.EqualTo("Role: dev"));
		Assert.That(table.Rows.Single().Single(), Is.EqualTo("Read [Exact key]: /a"));
	}

	private static void AssertBounds(RecordingTerminal terminal)
	{
		foreach (var (top, left, text) in terminal.Writes)
		{
			Assert.That(left, Is.GreaterThanOrEqualTo(0), $"write at ({top}, {left})");
			Assert.That(top, Is.GreaterThanOrEqualTo(0), $"write at ({top}, {left})");

			foreach (var line in text.Split('\n'))
				Assert.That(terminal.GetVisibleLength(line), Is.LessThanOrEqualTo(terminal.WindowWidth), $"line exceeds width: {line}");
		}

		foreach (var (left, top) in terminal.CursorSets)
		{
			Assert.That(left, Is.GreaterThanOrEqualTo(0));
			Assert.That(top, Is.GreaterThanOrEqualTo(0));
		}
	}

	private sealed class Harness
	{
		public readonly RecordingTerminal Terminal;
		public readonly ConnectionSession Session = new();
		public readonly StatusBar StatusBar;
		public readonly ScreenLayout Layout;
		public readonly KeyBrowseControl Control;

		public Harness(int width, int height)
		{
			Terminal = new RecordingTerminal { WindowWidth = width, WindowHeight = height };

			var localization = new EnglishLocalization();

			StatusBar = new StatusBar(Terminal, Terminal, Terminal, new StubAppInfo(), Session, localization);
			Layout = new ScreenLayout(Terminal, Terminal, StatusBar, new Header(Terminal));

			var browseLayout = new KeyBrowseLayout(Terminal, Terminal, Terminal, localization);

			Control = new KeyBrowseControl(Terminal, Terminal, Terminal, StatusBar, browseLayout, Layout, Session);
		}
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
