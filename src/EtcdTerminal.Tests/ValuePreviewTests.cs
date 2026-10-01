using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Terminal;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ValuePreviewTests
{
	[Test]
	public void Preview_ShortValue_ReturnsUnchanged()
	{
		Assert.That(ValuePreview.Preview("abc", 10), Is.EqualTo("abc"));
		Assert.That(ValuePreview.Preview("abc", 3), Is.EqualTo("abc"));
	}

	[Test]
	public void Preview_LongValue_KeepsEllipsisInsideBudget()
	{
		var preview = ValuePreview.Preview("abcdefgh", 5);

		Assert.That(preview, Is.EqualTo("abcd\u2026"));
		Assert.That(DisplayCells.Width(preview), Is.LessThanOrEqualTo(5));
	}

	[Test]
	public void Preview_NormalizesControlsForDisplayOnly()
	{
		Assert.That(ValuePreview.Preview("a\nb", 10), Is.EqualTo("a\u23CEb"));
		Assert.That(ValuePreview.Preview("a\r\nb", 10), Is.EqualTo("a\u23CEb"));
		Assert.That(ValuePreview.Preview("a\tb", 10), Is.EqualTo("a\u2192b"));
		Assert.That(ValuePreview.Preview("a\0b", 10), Is.EqualTo("a\uFFFDb"));
	}

	[Test]
	public void Preview_WideChars_TruncatesByCells()
	{
		var preview = ValuePreview.Preview("\u4e2d\u6587 ab", 5);

		Assert.That(DisplayCells.Width(preview), Is.LessThanOrEqualTo(5));
		Assert.That(preview, Does.EndWith("\u2026"));
	}

	[Test]
	public void Preview_NeverSplitsSurrogatePair()
	{
		var preview = ValuePreview.Preview("ab \U0001F600 cd", 4);

		Assert.That(DisplayCells.Width(preview), Is.LessThanOrEqualTo(4));
		Assert.That(preview, Does.EndWith("\u2026"));
	}

	[Test]
	public void Preview_ZeroOrNarrowWidth_IsSafe()
	{
		Assert.That(ValuePreview.Preview("abc", 0), Is.EqualTo(string.Empty));
		Assert.That(ValuePreview.Preview("abc", -3), Is.EqualTo(string.Empty));
		Assert.That(DisplayCells.Width(ValuePreview.Preview("abc", 1)), Is.LessThanOrEqualTo(1));
	}
}
