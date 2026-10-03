using EtcdTerminal.App.Components;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class DisplayTextTests
{
	[Test]
	public void Sanitize_PlainText_IsReturnedUnchanged()
	{
		Assert.That(DisplayText.Sanitize("abc"), Is.EqualTo("abc"));
		Assert.That(DisplayText.Sanitize("\u4e2d\u6587 ab"), Is.EqualTo("\u4e2d\u6587 ab"));
	}

	[Test]
	public void Sanitize_NormalizesControlsForDisplayOnly()
	{
		Assert.That(DisplayText.Sanitize("a\nb"), Is.EqualTo("a\u23CEb"));
		Assert.That(DisplayText.Sanitize("a\r\nb"), Is.EqualTo("a\u23CEb"));
		Assert.That(DisplayText.Sanitize("a\tb"), Is.EqualTo("a\u2192b"));
		Assert.That(DisplayText.Sanitize("a\0b"), Is.EqualTo("a\uFFFDb"));
	}
}
