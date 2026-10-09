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
public sealed class ImportParseFailureNoticeTests
{
	[Test]
	public void Show_InvalidJson_ReportsTheInvalidJsonError()
	{
		var harness = new Harness();

		harness.Keys.Press(ConsoleKey.Enter);

		harness.Notice.Show(new(ImportParseFailureKind.InvalidJson, "The input does not contain a valid JSON."));

		Assert.That(CanvasText(harness), Does.Contain("Invalid JSON: The input does not contain a valid JSON."));
	}

	[Test]
	public void Show_NoKeys_ReportsThatThereAreNoKeys()
	{
		var harness = new Harness();

		harness.Keys.Press(ConsoleKey.Enter);

		harness.Notice.Show(new(ImportParseFailureKind.NoKeys, null));

		Assert.That(CanvasText(harness), Does.Contain("No keys found in JSON."));
	}

	private static string CanvasText(Harness harness) =>
		string.Join('\n', harness.Canvas.Blocks.OfType<TextBlock>().SelectMany(block => block.Lines.Select(LineText.Of)));

	private sealed class Harness
	{
		public readonly FakeScreenCanvas Canvas = new();
		public readonly FakeKeyReader Keys = new();
		public readonly ImportParseFailureNotice Notice;

		public Harness()
		{
			LocalizationCatalog localization = new();
			var statusBar = new StatusBar(new StubAppInfo(), new ConnectionSession(), localization);
			var screen = new Screen(Canvas, new Header(), statusBar);

			Notice = new ImportParseFailureNotice(new Message(screen, Keys, localization), localization);
		}
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
