using System.Reflection;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens.Permissions;
using EtcdTerminal.App.Screens.Roles;
using EtcdTerminal.App.Screens.Users;
using EtcdTerminal.Permissions;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Roles;
using EtcdTerminal.Users;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class PermissionDisplayTests
{
	[Test]
	public void RoleList_BuildsTitledTableWithDistinctScopes()
	{
		var roles = new[]
		{
			new EtcdRole
			{
				Name = "dev",
				Permissions =
				[
					new EtcdPermission { Type = PermissionType.Read, KeyPrefix = "/a", RangeEnd = string.Empty },
					new EtcdPermission { Type = PermissionType.Write, KeyPrefix = "/p", RangeEnd = PermissionRange.PrefixRangeEnd("/p") },
					new EtcdPermission { Type = PermissionType.ReadWrite, KeyPrefix = "x", RangeEnd = "z" },
					new EtcdPermission { Type = PermissionType.Read, KeyPrefix = "m", RangeEnd = "\0" }
				]
			}
		};

		var body = new RoleListLayout(new EnglishLocalization()).Body(roles);

		Assert.That(body[0].Kind, Is.EqualTo(PanelKind.Title));
		Assert.That(body[0].Lines.Single().Text, Is.EqualTo("Role: dev"));
		Assert.That(body[1].Kind, Is.EqualTo(PanelKind.Table));
		Assert.That(body[1].Lines.Select(line => line.Text), Is.EqualTo([
			"Permissions",
			"Read [Exact key]: /a",
			"Write [Prefix]: /p",
			"ReadWrite [Range]: [x, z)",
			"Read [Range]: [m, \u221E)"
		]));
	}

	[Test]
	public void RoleList_BuildsAllKeysGrantDistinctly()
	{
		var roles = new[]
		{
			new EtcdRole
			{
				Name = "root",
				Permissions = [new EtcdPermission { Type = PermissionType.ReadWrite, KeyPrefix = "\0", RangeEnd = "\0" }]
			}
		};

		var body = new RoleListLayout(new EnglishLocalization()).Body(roles);

		Assert.That(body[1].Lines[1].Text, Is.EqualTo("ReadWrite [Prefix]: All keys"));
	}

	[Test]
	public void PermissionView_BuildsLocalizedUserTitleAndSharedFormat()
	{
		var users = new[] { new EtcdUser { Username = "bob", Roles = ["dev"] } };
		var roles = new[]
		{
			new EtcdRole
			{
				Name = "dev",
				Permissions = [new EtcdPermission { Type = PermissionType.Read, KeyPrefix = "/a", RangeEnd = string.Empty }]
			}
		};

		var body = new PermissionViewLayout(new EnglishLocalization()).Body(users, roles);

		Assert.That(body[0].Kind, Is.EqualTo(PanelKind.Title));
		Assert.That(body[0].Lines.Single().Text, Is.EqualTo("User: bob"));
		Assert.That(body[1].Kind, Is.EqualTo(PanelKind.Table));

		IReadOnlyList<IReadOnlyList<string>> expected =
		[
			["Role", "Permissions"],
			["dev", "Read [Exact key]: /a"]
		];

		Assert.That(Cells(body[1]), Is.EqualTo(expected));
	}

	[Test]
	public void PermissionView_EmptyDirectoryBuildsOneLocalizedNotice()
	{
		var body = new PermissionViewLayout(new EnglishLocalization()).Body([], []);

		Assert.That(body, Has.Count.EqualTo(1));
		Assert.That(body[0].Kind, Is.EqualTo(PanelKind.Default));
		Assert.That(body[0].Lines.Single().Text, Is.EqualTo("No users or roles found."));
	}

	[Test]
	public void UserList_BuildsLocalizedColumnsAndEmptyState()
	{
		var localization = new EnglishLocalization();
		var body = new UserListLayout(localization).Body([new EtcdUser { Username = "alice", Roles = ["dev", "ops"] }]);

		Assert.That(body.Single().Kind, Is.EqualTo(PanelKind.Table));

		IReadOnlyList<IReadOnlyList<string>> expected =
		[
			["Username", "Roles"],
			["alice", "dev, ops"]
		];

		Assert.That(Cells(body.Single()), Is.EqualTo(expected));
		Assert.That(new UserListLayout(localization).Body([]).Single().Lines.Single().Text, Is.EqualTo("No users found."));
	}

	[Test]
	public void MarkedLocalization_UsesLabelsNotHardcodedEnglish()
	{
		var localization = MarkingLocalization.Create();
		var users = new[] { new EtcdUser { Username = "bob", Roles = ["dev"] } };
		var roles = new[]
		{
			new EtcdRole
			{
				Name = "dev",
				Permissions =
				[
					new EtcdPermission { Type = PermissionType.Read, KeyPrefix = "/a", RangeEnd = string.Empty },
					new EtcdPermission { Type = PermissionType.Write, KeyPrefix = "\0", RangeEnd = "\0" }
				]
			}
		};

		List<PanelModel> body = [.. new PermissionViewLayout(localization).Body(users, roles)];
		body.AddRange(new RoleListLayout(localization).Body(roles));

		var output = Joined([.. body.Where(panel => panel.Kind == PanelKind.Table)]);
		var titles = Joined([.. body.Where(panel => panel.Kind == PanelKind.Title)]);

		Assert.That(output, Does.Contain("MK_READ [MK_KEY]: /a"));
		Assert.That(output, Does.Contain("MK_WRITE [MK_PREFIX]: MK_ALL"));
		Assert.That(titles, Does.Contain("MK_USER: bob"));
		Assert.That(titles, Does.Contain("MK_ROLE: dev"));
		Assert.That(output, Does.Not.Contain("Exact key"));
		Assert.That(output, Does.Not.Contain("User:"));
		Assert.That(output, Does.Not.Contain("Role:"));
		Assert.That(output, Does.Not.Contain("Read ["));
		Assert.That(output, Does.Not.Contain('\0'));
	}

	[Test]
	public void SameStartDifferentEnds_RenderDifferently()
	{
		var localization = new EnglishLocalization();
		var first = PermissionDisplay.For(new EtcdPermission { Type = PermissionType.Read, KeyPrefix = "a", RangeEnd = "c" }, localization);
		var second = PermissionDisplay.For(new EtcdPermission { Type = PermissionType.Read, KeyPrefix = "a", RangeEnd = "d" }, localization);
		var open = PermissionDisplay.For(new EtcdPermission { Type = PermissionType.Read, KeyPrefix = "a", RangeEnd = "\0" }, localization);

		Assert.That(first, Is.EqualTo("Read [Range]: [a, c)"));
		Assert.That(second, Is.EqualTo("Read [Range]: [a, d)"));
		Assert.That(first, Is.Not.EqualTo(second));
		Assert.That(open, Is.EqualTo("Read [Range]: [a, \u221E)"));
	}

	private static IReadOnlyList<IReadOnlyList<string>> Cells(PanelModel panel) =>
		[.. panel.Lines.Select(line => line.Spans.Select(span => span.Text).ToList())];

	private static string Joined(IReadOnlyList<PanelModel> panels) =>
		string.Join("\n", panels.SelectMany(panel => panel.Lines).Select(line => line.Text));

	private class MarkingLocalization : DispatchProxy
	{
		private readonly EnglishLocalization _inner = new();

		public static ILocalization Create() => DispatchProxy.Create<ILocalization, MarkingLocalization>();

		protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
		{
			"get_ScopeKey" => "MK_KEY",
			"get_ScopePrefix" => "MK_PREFIX",
			"get_ScopeRange" => "MK_RANGE",
			"get_Read" => "MK_READ",
			"get_Write" => "MK_WRITE",
			"get_ReadWrite" => "MK_READWRITE",
			"get_User" => "MK_USER",
			"get_Role" => "MK_ROLE",
			"get_AllKeys" => "MK_ALL",
			_ => targetMethod!.Invoke(_inner, args)
		};
	}
}
