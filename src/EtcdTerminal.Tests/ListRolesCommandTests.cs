using EtcdTerminal.App.Components;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens.Roles;
using EtcdTerminal.App.Screens.Roles.Commands;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Presentation;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ListRolesCommandTests
{
	[Test]
	public async Task ExecuteAsync_EscapeDuringLoad_WarnsAndDoesNotOpenTheBrowser()
	{
		var harness = new Harness();

		harness.Keys.Press(ConsoleKey.Escape, ConsoleKey.Enter);

		await harness.Command.ExecuteAsync();

		Assert.Multiple(() =>
		{
			Assert.That(CanvasText(harness), Does.Contain("Operation cancelled."));
			Assert.That(harness.Live.Frames, Is.Empty, "a cancelled load never opens the list page");
		});
	}

	private static string CanvasText(Harness harness) =>
		string.Join('\n', harness.Canvas.Blocks.OfType<TextBlock>().SelectMany(block => block.Lines.Select(LineText.Of)));

	private sealed class Harness
	{
		public readonly FakeKeyReader Keys = new();
		public readonly FakeScreenCanvas Canvas = new();
		public readonly FakeLiveFrame Live = new();
		public readonly FakeRoleAdmin Admin = new() { HoldRoles = true };
		public readonly ListRolesCommand Command;

		public Harness()
		{
			LocalizationCatalog localization = new();
			var settings = new AppSettingsStore(new FakeSettingsRepository());
			var statusBar = new StatusBar(new StubAppInfo(), new ConnectionSession(), localization);
			var screen = new Screen(Canvas, new Header(), statusBar);
			var spinner = new Spinner(Keys, new FakeStatusIndicator());
			var load = new CancellableLoad(spinner, new Message(screen, Keys, localization), localization);
			var browser = new ListBrowser(new ListView(screen, new BrowseLayout(localization)), Live, Keys, settings);

			Command = new ListRolesCommand(Admin, load, new RoleListLayout(localization), browser);
		}
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
