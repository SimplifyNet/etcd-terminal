using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Configuration;
using EtcdTerminal.Security;
using EtcdTerminal.Session;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class MainMenuItemsTests
{
	[Test]
	public void Build_FullCapabilities_ListsEveryActionInThePreviousOrder()
	{
		var harness = new Harness(fullCapabilities: true);

		var offered = harness.Items.Build();

		Assert.Multiple(() =>
		{
			Assert.That(offered.Select(i => i.Id), Is.EqualTo(new[]
			{
				MainMenuAction.BrowseKeys,
				MainMenuAction.CreateKey,
				MainMenuAction.ImportJson,
				MainMenuAction.ManageUsers,
				MainMenuAction.ManageRoles,
				MainMenuAction.ListPermissions,
				MainMenuAction.Disconnect
			}));
			Assert.That(offered.Select(i => i.Label), Is.EqualTo(new[]
			{
				"Browse Keys",
				"Create Key",
				"Import JSON",
				"Manage Users",
				"Manage Roles",
				"List Permissions",
				"Disconnect"
			}));
		});
	}

	[Test]
	public void Build_UnavailableEntries_AreFilteredOutButDisconnectStays()
	{
		var harness = new Harness(fullCapabilities: false);

		var offered = harness.Items.Build();

		Assert.Multiple(() =>
		{
			Assert.That(offered.Select(i => i.Id), Is.EqualTo(new[] { MainMenuAction.Disconnect }));
			Assert.That(offered.Select(i => i.Label), Is.EqualTo(new[] { "Disconnect" }));
		});
	}

	[Test]
	public void Find_ReturnsTheEntryWithTheRequestedAction()
	{
		var harness = new Harness(fullCapabilities: true);

		Assert.Multiple(() =>
		{
			Assert.That(harness.Items.Find(MainMenuAction.ImportJson), Is.SameAs(harness.Entries[2]));
			Assert.That(harness.Items.Find(MainMenuAction.Disconnect), Is.Null);
		});
	}

	private sealed class Harness
	{
		public readonly List<StubEntry> Entries = [];
		public readonly MainMenuItems Items;

		public Harness(bool fullCapabilities)
		{
			var session = new ConnectionSession();

			session.Start(new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://localhost:2379" },
				fullCapabilities ? UserCapabilities.Unrestricted : new UserCapabilities());

			foreach (var action in new[]
			{
				MainMenuAction.BrowseKeys,
				MainMenuAction.CreateKey,
				MainMenuAction.ImportJson,
				MainMenuAction.ManageUsers,
				MainMenuAction.ManageRoles,
				MainMenuAction.ListPermissions
			})
				Entries.Add(new StubEntry(action, fullCapabilities));

			Items = new MainMenuItems(Entries, session, new MainMenuLabels(new LocalizationCatalog()));
		}
	}

	private sealed class StubEntry(MainMenuAction action, bool available) : IMainMenuEntry
	{
		public MainMenuAction Action => action;

		public bool IsAvailable(UserCapabilities capabilities) => available;

		public Task ShowAsync() => Task.CompletedTask;
	}
}
