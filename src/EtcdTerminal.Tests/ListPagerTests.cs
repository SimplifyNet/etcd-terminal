using EtcdTerminal.App.Components;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ListPagerTests
{
	[Test]
	public void Filter_IsCaseInsensitiveAcrossTheWholeRow()
	{
		var pager = CreatePager([["user alice", "dev"], ["user bob", "ops"]]);

		pager.Filter("ALICE");

		Assert.That(pager.FilteredCount, Is.EqualTo(1));
		Assert.That(pager.GetPage(0, 10).Select(row => row[0]), Is.EqualTo(new[] { "user alice" }));
	}

	[Test]
	public void Filter_EmptyQuery_RestoresEverySourceRow()
	{
		var pager = CreatePager([["one"], ["two"]]);

		pager.Filter("tw");
		pager.Filter("");

		Assert.That(pager.FilteredCount, Is.EqualTo(2));
	}

	[Test]
	public void GetTotalPages_IsOneForEmpty()
	{
		var pager = CreatePager([]);

		Assert.That(pager.GetTotalPages(10), Is.EqualTo(1));
	}

	[Test]
	public void GetPage_ReturnsTheLastPartialPage()
	{
		var pager = CreatePager([["1"], ["2"], ["3"]]);

		var page = pager.GetPage(1, 2);

		Assert.That(page.Select(row => row[0]), Is.EqualTo(new[] { "3" }));
		Assert.That(pager.GetTotalPages(2), Is.EqualTo(2));
	}

	private static ListPager<IReadOnlyList<string>> CreatePager(IReadOnlyList<IReadOnlyList<string>> source)
	{
		var pager = new ListPager<IReadOnlyList<string>>(source, row => string.Join(' ', row));

		return pager;
	}
}
