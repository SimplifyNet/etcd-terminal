using System.Reflection;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens.Permissions;
using EtcdTerminal.App.Screens.Roles;
using EtcdTerminal.Localization;
using EtcdTerminal.Permissions;
using EtcdTerminal.Roles;
using EtcdTerminal.Tests.Fakes;
using EtcdTerminal.Users;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class PermissionDisplayTests
{
	[Test]
	public void RoleList_RendersDistinctScopesInCapturedTable()
	{
		var terminal = new FakeTerminal();
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

		RoleListRenderer.Render(terminal, new EnglishLocalization(), roles);

		var table = terminal.Tables.Single();

		Assert.That(table.Title, Is.EqualTo("Role: dev"));
		Assert.That(table.Columns, Is.EqualTo(["Permissions"]));
		Assert.That(table.Rows.Select(r => r.Single()), Is.EqualTo([
			"Read [Exact key]: /a",
			"Write [Prefix]: /p",
			"ReadWrite [Range]: [x, z)",
			"Read [Range]: [m, \u221E)"
		]));
	}

	[Test]
	public void RoleList_RendersAllKeysGrantDistinctly()
	{
		var terminal = new FakeTerminal();
		var roles = new[]
		{
			new EtcdRole
			{
				Name = "root",
				Permissions = [new EtcdPermission { Type = PermissionType.ReadWrite, KeyPrefix = "\0", RangeEnd = "\0" }]
			}
		};

		RoleListRenderer.Render(terminal, new EnglishLocalization(), roles);

		var row = terminal.Tables.Single().Rows.Single().Single();

		Assert.That(row, Is.EqualTo("ReadWrite [Prefix]: All keys"));
	}

	[Test]
	public void PermissionView_RendersLocalizedUserTitleAndSharedFormat()
	{
		var terminal = new FakeTerminal();
		var users = new[] { new EtcdUser { Username = "bob", Roles = ["dev"] } };
		var roles = new[]
		{
			new EtcdRole
			{
				Name = "dev",
				Permissions = [new EtcdPermission { Type = PermissionType.Read, KeyPrefix = "/a", RangeEnd = string.Empty }]
			}
		};

		PermissionViewRenderer.Render(terminal, new EnglishLocalization(), users, roles);

		var table = terminal.Tables.Single();

		Assert.That(table.Title, Is.EqualTo("User: bob"));
		Assert.That(table.Columns, Is.EqualTo(["Role", "Permissions"]));
		Assert.That(table.Rows.Single(), Is.EqualTo((IReadOnlyList<string>)["dev", "Read [Exact key]: /a"]));
	}

	[Test]
	public void MarkedLocalization_UsesLabelsNotHardcodedEnglish()
	{
		var terminal = new FakeTerminal();
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

		PermissionViewRenderer.Render(terminal, localization, users, roles);
		RoleListRenderer.Render(terminal, localization, roles);

		var output = string.Join("\n", terminal.Tables.SelectMany(t => t.Rows.SelectMany(r => r)));
		var titles = string.Join("\n", terminal.Tables.Select(t => t.Title));

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
