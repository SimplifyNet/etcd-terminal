using EtcdTerminal.App.Components;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens.Keys.Import;
using EtcdTerminal.Environment;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ImportReportTests
{
	private static readonly IReadOnlyList<KeyValuePair<string, string>> Entries =
	[
		new("/a", "1"),
		new("/b", "2")
	];

	[Test]
	public void ShowCancelled_WithNothingDone_ReportsThePlainCancellation()
	{
		var harness = new Harness();

		harness.Keys.Press(ConsoleKey.Enter);

		harness.Report.ShowCancelled(Entries, new KeyImportResult(0, 0, 0));

		Assert.Multiple(() =>
		{
			Assert.That(CanvasText(harness), Does.Contain("Import cancelled."));
			Assert.That(CanvasText(harness), Does.Not.Contain("Confirmed:"), "nothing was written, so there is no partial result");
		});
	}

	[Test]
	public void ShowCancelled_WithProgress_ReportsTheUnconfirmedEntryAndTheCounts()
	{
		var harness = new Harness();

		harness.Keys.Press(ConsoleKey.Enter);

		harness.Report.ShowCancelled(Entries, new KeyImportResult(1, 0, 0));

		Assert.Multiple(() =>
		{
			Assert.That(CanvasText(harness), Does.Contain("Import cancelled."));
			Assert.That(CanvasText(harness), Does.Contain("Entry '/b' may have committed on the server; its outcome is unconfirmed: Operation cancelled."));
			Assert.That(CanvasText(harness), Does.Contain("Confirmed: 1 created, 0 overwritten, 0 failed."));
		});
	}

	private static string CanvasText(Harness harness) =>
		string.Join('\n', harness.Canvas.Blocks.OfType<TextBlock>().SelectMany(block => block.Lines.Select(LineText.Of)));

	private sealed class Harness
	{
		public readonly FakeScreenCanvas Canvas = new();
		public readonly FakeKeyReader Keys = new();
		public readonly ImportReport Report;

		public Harness()
		{
			var localization = new EnglishLocalization();
			var statusBar = new StatusBar(new StubAppInfo(), new ConnectionSession(), localization);
			var screen = new Screen(Canvas, new Header(), statusBar);

			Report = new ImportReport(new Message(screen, Keys, localization), localization);
		}
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
