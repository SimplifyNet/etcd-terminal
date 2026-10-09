using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens.Permissions;
using EtcdTerminal.App.Screens.Roles;
using EtcdTerminal.App.Screens.Users;
using EtcdTerminal.Permissions;
using EtcdTerminal.Roles;
using EtcdTerminal.Users;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ListLayoutTests
{
	[Test]
	public void UserListLayout_Rows_JoinsRolesAndFallsBackToNone()
	{
		var rows = new UserListLayout(new LocalizationCatalog()).Rows(
		[
			new EtcdUser { Username = "alice", Roles = ["dev", "ops"] },
			new EtcdUser { Username = "bob" }
		]);

		Assert.That(rows, Has.Count.EqualTo(2));
		Assert.That(rows[0].Select(cell => cell.Text), Is.EqualTo(new[] { "alice", "dev, ops" }));
		Assert.That(rows[1].Select(cell => cell.Text), Is.EqualTo(new[] { "bob", "none" }));
	}

	[Test]
	public void UserListLayout_Headers_AreTheFramedTablesColumns()
	{
		var headers = new UserListLayout(new LocalizationCatalog()).Headers();

		Assert.That(headers.Select(cell => cell.Text), Is.EqualTo(new[] { "Username", "Roles" }));
	}

	[Test]
	public void RoleListLayout_Rows_OneRowPerPermissionAndAPlaceholderForEmptyRoles()
	{
		var rows = new RoleListLayout(new LocalizationCatalog()).Rows(
		[
			new EtcdRole { Name = "dev", Permissions = [Permission("/a")] },
			new EtcdRole { Name = "empty" }
		]);

		Assert.That(rows, Has.Count.EqualTo(2));
		Assert.That(rows[0].Select(cell => cell.Text), Is.EqualTo(new[] { "dev", "Read [Prefix]: /a" }));
		Assert.That(rows[1].Select(cell => cell.Text), Is.EqualTo(new[] { "empty", "no permissions" }));
	}

	[Test]
	public void RoleListLayout_Headers_NameTheRoleAndPermissionColumns()
	{
		var headers = new RoleListLayout(new LocalizationCatalog()).Headers();

		Assert.That(headers.Select(cell => cell.Text), Is.EqualTo(new[] { "Role", "Permission" }));
	}

	[Test]
	public void PermissionListLayout_Rows_ExpandsEveryRoleOfEveryUser()
	{
		var rows = new PermissionListLayout(new LocalizationCatalog()).Rows(
		[
			new EtcdUser { Username = "alice", Roles = ["dev"] },
			new EtcdUser { Username = "bob" },
			new EtcdUser { Username = "carol", Roles = ["ghost"] }
		],
		[
			new EtcdRole { Name = "dev", Permissions = [Permission("/a")] }
		]);

		Assert.That(rows, Has.Count.EqualTo(3));
		Assert.That(rows[0].Select(cell => cell.Text), Is.EqualTo(new[] { "alice", "dev", "Read [Prefix]: /a" }));
		Assert.That(rows[1].Select(cell => cell.Text), Is.EqualTo(new[] { "bob", "no roles", "-" }));
		Assert.That(rows[2].Select(cell => cell.Text), Is.EqualTo(new[] { "carol", "ghost", "no permissions" }));
	}

	[Test]
	public void PermissionListLayout_Headers_NameTheUserRolesAndPermissionColumns()
	{
		var headers = new PermissionListLayout(new LocalizationCatalog()).Headers();

		Assert.That(headers.Select(cell => cell.Text), Is.EqualTo(new[] { "User", "Role", "Permission" }));
	}

	private static EtcdPermission Permission(string key) =>
		new() { Type = PermissionType.Read, KeyPrefix = key, RangeEnd = PermissionRange.PrefixRangeEnd(key) };
}
