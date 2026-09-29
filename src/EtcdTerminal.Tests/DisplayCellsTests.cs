using EtcdTerminal.Terminal;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class DisplayCellsTests
{
	[Test]
	public void Width_AsciiCountsOnePerChar()
	{
		Assert.That(DisplayCells.Width("abc"), Is.EqualTo(3));
		Assert.That(DisplayCells.Width(string.Empty), Is.EqualTo(0));
	}

	[Test]
	public void Width_CjkCountsTwoPerChar()
	{
		Assert.That(DisplayCells.Width("\u4e2d\u6587"), Is.EqualTo(4));
	}

	[Test]
	public void Width_EmojiCountsTwo()
	{
		Assert.That(DisplayCells.Width("\U0001F600"), Is.EqualTo(2));
	}

	[Test]
	public void Width_CombiningMarkCostsNothing()
	{
		Assert.That(DisplayCells.Width("e\u0301"), Is.EqualTo(1));
	}

	[Test]
	public void Width_ControlCostsNothing()
	{
		Assert.That(DisplayCells.Width("a\nb\t"), Is.EqualTo(2));
	}

	[Test]
	public void Width_LoneSurrogateCountsOne()
	{
		Assert.That(DisplayCells.Width("a\ud800b"), Is.EqualTo(3));
	}
}
