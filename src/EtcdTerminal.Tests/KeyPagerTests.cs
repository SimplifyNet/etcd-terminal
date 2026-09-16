using EtcdTerminal.App.Screens.Keys;
using EtcdTerminal.Keys;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class KeyPagerTests
{
	[Test]
	public void Filter_IsCaseInsensitive()
	{
		var pager = CreatePager([new EtcdKeyValue { Key = "/App/Name", Value = "x" }, new EtcdKeyValue { Key = "/other", Value = "y" }]);

		pager.Filter("app");

		Assert.That(pager.GetPage(0, 10).Select(kv => kv.Key), Is.EquivalentTo(["/App/Name"]));
	}

	[Test]
	public void Filter_MatchesValueAsWellAsKey()
	{
		var pager = CreatePager([new EtcdKeyValue { Key = "/a", Value = "needle-here" }, new EtcdKeyValue { Key = "/b", Value = "plain" }]);

		pager.Filter("needle");

		Assert.That(pager.GetPage(0, 10).Select(kv => kv.Key), Is.EquivalentTo(["/a"]));
	}

	[Test]
	public void GetTotalPages_ReturnsOneForEmpty()
	{
		var pager = CreatePager([]);

		Assert.That(pager.GetTotalPages(10), Is.EqualTo(1));
	}

	[Test]
	public void GetPage_ReturnsLastPartialPage()
	{
		var pager = CreatePager([
			new EtcdKeyValue { Key = "/1", Value = "a" },
			new EtcdKeyValue { Key = "/2", Value = "b" },
			new EtcdKeyValue { Key = "/3", Value = "c" }
		]);

		var page = pager.GetPage(1, 2);

		Assert.That(page.Select(kv => kv.Key), Is.EquivalentTo(["/3"]));
		Assert.That(pager.GetTotalPages(2), Is.EqualTo(2));
	}

	private static KeyPager CreatePager(IReadOnlyList<EtcdKeyValue> keys)
	{
		var pager = new KeyPager();

		pager.SetSource(keys);

		return pager;
	}
}
