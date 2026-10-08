using EtcdTerminal.App.Components;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens.Keys.Import;
using EtcdTerminal.Environment;
using EtcdTerminal.Presentation;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ImportEntriesParserTests
{
	[Test]
	public void Parse_InvalidJson_ReportsTheInvalidJsonError()
	{
		var harness = new Harness();

		harness.Keys.Press(ConsoleKey.Enter);

		var entries = harness.Parser.Parse(new ImportSource(":", "", "{bad"));

		Assert.Multiple(() =>
		{
			Assert.That(entries, Is.Null);
			Assert.That(CanvasText(harness), Does.Contain("Invalid JSON:"));
		});
	}

	[Test]
	public void Parse_EmptyDocument_ReportsThatThereAreNoKeys()
	{
		var harness = new Harness();

		harness.Keys.Press(ConsoleKey.Enter);

		var entries = harness.Parser.Parse(new ImportSource(":", "", "{}"));

		Assert.Multiple(() =>
		{
			Assert.That(entries, Is.Null);
			Assert.That(CanvasText(harness), Does.Contain("No keys found in JSON."));
		});
	}

	private static string CanvasText(Harness harness) =>
		string.Join('\n', harness.Canvas.Blocks.OfType<TextBlock>().SelectMany(block => block.Lines.Select(LineText.Of)));

	private sealed class Harness
	{
		public readonly FakeScreenCanvas Canvas = new();
		public readonly FakeKeyReader Keys = new();
		public readonly ImportEntriesParser Parser;

		public Harness()
		{
			var localization = new EnglishLocalization();
			var statusBar = new StatusBar(new StubAppInfo(), new ConnectionSession(), localization);
			var screen = new Screen(Canvas, new Header(), statusBar);

			Parser = new ImportEntriesParser(new Message(screen, Keys, localization), localization);
		}
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
