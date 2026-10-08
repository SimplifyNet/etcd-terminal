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
	public void RoleList_BuildsOneRowPerPermissionWithDistinctScopes()
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

		var layout = new RoleListLayout(new EnglishLocalization());

		IReadOnlyList<IReadOnlyList<string>> expected =
		[
			["dev", "Read [Exact key]: /a"],
			["dev", "Write [Prefix]: /p"],
			["dev", "ReadWrite [Range]: [x, z)"],
			["dev", "Read [Range]: [m, \u221E)"]
		];

		Assert.That(Texts(layout.Headers()), Is.EqualTo(new[] { "Role", "Permission" }));
		Assert.That(layout.Headers().Select(span => span.Role), Is.All.EqualTo(TextRole.Accent));
		Assert.That(Texts(layout.Rows(roles)), Is.EqualTo(expected));
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

		var rows = new RoleListLayout(new EnglishLocalization()).Rows(roles);

		Assert.That(LineText.Of(rows[0]), Does.EndWith("ReadWrite [Prefix]: All keys"));
	}

	[Test]
	public void PermissionList_BuildsLocalizedColumnsAndSharedFormat()
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

		var layout = new PermissionListLayout(new EnglishLocalization());

		IReadOnlyList<IReadOnlyList<string>> expected = [["bob", "dev", "Read [Exact key]: /a"]];

		Assert.That(Texts(layout.Headers()), Is.EqualTo(new[] { "User", "Role", "Permission" }));
		Assert.That(layout.Headers().Select(span => span.Role), Is.All.EqualTo(TextRole.Accent));
		Assert.That(Texts(layout.Rows(users, roles)), Is.EqualTo(expected));
		Assert.That(layout.Rows([], []), Is.Empty);
	}

	[Test]
	public void UserList_BuildsLocalizedColumns()
	{
		var layout = new UserListLayout(new EnglishLocalization());

		IReadOnlyList<IReadOnlyList<string>> expected = [["alice", "dev, ops"]];

		Assert.That(Texts(layout.Headers()), Is.EqualTo(new[] { "Username", "Roles" }));
		Assert.That(layout.Headers().Select(span => span.Role), Is.All.EqualTo(TextRole.Accent));
		Assert.That(Texts(layout.Rows([new EtcdUser { Username = "alice", Roles = ["dev", "ops"] }])), Is.EqualTo(expected));
		Assert.That(layout.Rows([]), Is.Empty);
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

		var permissions = new PermissionListLayout(localization);
		var roleList = new RoleListLayout(localization);

		var headers = string.Join("\n", Texts(permissions.Headers()).Concat(Texts(roleList.Headers())));
		var output = string.Join("\n", permissions.Rows(users, roles).Concat(roleList.Rows(roles)).Select(LineText.Of));

		Assert.That(headers, Does.Contain("MK_USER"));
		Assert.That(headers, Does.Contain("MK_ROLE"));
		Assert.That(output, Does.Contain("MK_READ [MK_KEY]: /a"));
		Assert.That(output, Does.Contain("MK_WRITE [MK_PREFIX]: MK_ALL"));
		Assert.That(output, Does.Not.Contain("Exact key"));
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

	[Test]
	public void For_SanitizesControlCharactersInBounds()
	{
		var permission = new EtcdPermission { Type = PermissionType.Read, KeyPrefix = "\0a/b", RangeEnd = "\0c" };
		var line = PermissionDisplay.For(permission, new EnglishLocalization());

		Assert.Multiple(() =>
		{
			Assert.That(line, Is.EqualTo("Read [Range]: [\uFFFDa/b, \uFFFDc)"));
			Assert.That(permission.KeyPrefix, Is.EqualTo("\0a/b"));
			Assert.That(permission.RangeEnd, Is.EqualTo("\0c"));
		});
	}

	private static IReadOnlyList<string> Texts(IEnumerable<StyledText> spans) =>
		[.. spans.Select(span => span.Text)];

	private static IReadOnlyList<IReadOnlyList<string>> Texts(IEnumerable<IReadOnlyList<StyledText>> rows) =>
		[.. rows.Select(Texts)];

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
