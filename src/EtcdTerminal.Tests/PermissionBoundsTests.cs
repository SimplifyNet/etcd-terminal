using System.Text;
using EtcdTerminal.Permissions;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class PermissionBoundsTests
{
	[Test]
	public void PrefixRangeEnd_AsciiAppendsSuccessor()
	{
		Assert.That(PermissionRange.PrefixRangeEnd("/a"), Is.EqualTo("/b"));
		Assert.That(PermissionRange.PrefixRangeEnd("/a/"), Is.EqualTo("/a0"));
	}

	[Test]
	public void PrefixRangeEnd_EmptyAndAllKeysStayUnbounded()
	{
		Assert.That(PermissionRange.PrefixRangeEnd(string.Empty), Is.EqualTo("\0"));
		Assert.That(PermissionRange.PrefixRangeEnd("\0"), Is.EqualTo("\0"));
	}

	[Test]
	public void PrefixRangeEndBytes_SupplementaryCharacter_IncrementsInUtf8Order()
	{
		var end = PermissionRange.PrefixRangeEndBytes(Encoding.UTF8.GetBytes("t05e\U00010000"));

		Assert.That(end, Is.EqualTo(Encoding.UTF8.GetBytes("t05e\U00010001")));
	}

	[Test]
	public void PrefixRangeEndBytes_PrefixEndingInD7FF_IncrementsTrailingByte()
	{
		var end = PermissionRange.PrefixRangeEndBytes(Encoding.UTF8.GetBytes("t05p\uD7FF"));

		Assert.That(end, Is.EqualTo(new byte[] { 0x74, 0x30, 0x35, 0x70, 0xED, 0x9F, 0xC0 }));
	}

	[Test]
	public void ScopeOf_ClassifiesKeyPrefixRangeAndOpenEndedRanges()
	{
		Assert.That(PermissionRange.ScopeOf("/a", string.Empty), Is.EqualTo(PermissionScope.Key));
		Assert.That(PermissionRange.ScopeOf("/a", "/b"), Is.EqualTo(PermissionScope.Prefix));
		Assert.That(PermissionRange.ScopeOf("/a", "/c"), Is.EqualTo(PermissionScope.Range));
		Assert.That(PermissionRange.ScopeOf("/a", "\0"), Is.EqualTo(PermissionScope.Range));
		Assert.That(PermissionRange.ScopeOf("\0", "\0"), Is.EqualTo(PermissionScope.Prefix));
	}

	[Test]
	public void Covers_ExactKey_DoesNotMatchLongerKey()
	{
		var permission = new EtcdPermission { Type = PermissionType.Read, KeyPrefix = "/a", RangeEnd = string.Empty };

		Assert.That(permission.Covers("/a"), Is.True);
		Assert.That(permission.Covers("/ab"), Is.False);
	}

	[Test]
	public void Covers_BoundedRange_IncludesStartExcludesEnd()
	{
		var permission = new EtcdPermission { Type = PermissionType.Read, KeyPrefix = "a", RangeEnd = "c" };

		Assert.That(permission.Covers("a"), Is.True);
		Assert.That(permission.Covers("b"), Is.True);
		Assert.That(permission.Covers("c"), Is.False);
	}

	[Test]
	public void Covers_OpenEndedRange_MatchesTail()
	{
		var permission = new EtcdPermission { Type = PermissionType.Read, KeyPrefix = "m", RangeEnd = "\0" };

		Assert.That(permission.Covers("a"), Is.False);
		Assert.That(permission.Covers("m"), Is.True);
		Assert.That(permission.Covers("z"), Is.True);
	}

	[Test]
	public void Covers_SupplementaryRange_UsesByteOrderInsteadOfUtf16()
	{
		var permission = new EtcdPermission { Type = PermissionType.Read, KeyPrefix = "\U00010000", RangeEnd = "\U00020000" };

		Assert.That(permission.Covers("\uE000"), Is.False);
		Assert.That(permission.Covers("\U00010001"), Is.True);
	}
}
