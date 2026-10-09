using System.Text;
using EtcdTerminal.Infrastructure.Terminal;
using NUnit.Framework;
using Spectre.Console;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class SpectreConsoleHostTests
{
	[Test]
	public void Default_InitializesTheConsoleAsUtf8()
	{
		var console = SpectreConsoleHost.Default;

		Assert.Multiple(() =>
		{
			Assert.That(Console.OutputEncoding.CodePage, Is.EqualTo(Encoding.UTF8.CodePage));
			Assert.That(console.Profile.Encoding.CodePage, Is.EqualTo(Encoding.UTF8.CodePage));
		});
	}
}
