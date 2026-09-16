using System.Text;
using EtcdTerminal.App.Components;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class MultiLinePasteReaderTests
{
	[Test]
	public void CountLines_IgnoresBlankLines()
	{
		var buffer = new StringBuilder("{\n\n\"a\": 1\n}\n");

		Assert.That(MultiLinePasteReader.CountLines(buffer), Is.EqualTo(3));
	}
}
