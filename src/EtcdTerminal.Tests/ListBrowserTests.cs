using EtcdTerminal.App.Components;
using EtcdTerminal.App.Localization;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Presentation;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ListBrowserTests
{
	[Test]
	public void Show_ComposesBannerSearchTableAndPagination()
	{
		var harness = new Harness(2);

		harness.Keys.Press(ConsoleKey.Escape);

		harness.Show();

		var frame = harness.Live.Frames.Single();

		Assert.Multiple(() =>
		{
			Assert.That(frame.Body, Has.Count.EqualTo(6));
			Assert.That(frame.Body[0], Is.InstanceOf<BannerBlock>());
			Assert.That(((TextBlock)frame.Body[1]).Band, Is.True, "the filter line is a band");
			Assert.That(((TextBlock)frame.Body[2]).Lines.Single(), Is.Empty, "one blank row between the filter and the table");
			Assert.That(((TextBlock)frame.Body[4]).Lines.Single(), Is.Empty, "one blank row between the table and the pagination");
			Assert.That(((TextBlock)frame.Body[5]).Band, Is.True, "the pagination line is a band");
			Assert.That(harness.Live.Ends, Is.EqualTo(new[] { LiveFrameEnd.Clear }), "the released frame clears its viewport");
		});

		var table = (TableBlock)frame.Body[3];

		Assert.Multiple(() =>
		{
			Assert.That(table.IsFramed, Is.False, "a view-only page keeps the table borderless");
			Assert.That(table.Pointer, Is.False, "a view-only page carries no selection pointer");
			Assert.That(table.Header.Select(cell => cell.Text), Is.EqualTo(new[] { "User", "Role" }));
			Assert.That(table.Rows, Has.Count.EqualTo(2));
		});
	}

	[Test]
	public void Show_RightArrowMovesToTheNextPage()
	{
		var harness = new Harness(45);

		harness.Keys.Press(ConsoleKey.RightArrow, ConsoleKey.Escape);

		harness.Show();

		var second = (TableBlock)harness.Live.Frames.Last().Body[3];

		Assert.Multiple(() =>
		{
			Assert.That(harness.Live.Frames, Has.Count.EqualTo(2), "one redraw after the page turn");
			Assert.That(second.Rows, Has.Count.EqualTo(15), "the last partial page");
			Assert.That(second.Rows[0][0].Text, Is.EqualTo("row30"));
		});
	}

	[Test]
	public void Show_DownArrowTurnsThePageLikeTheMouseWheel()
	{
		var harness = new Harness(45);

		harness.Keys.Press(ConsoleKey.DownArrow, ConsoleKey.Escape);

		harness.Show();

		var second = (TableBlock)harness.Live.Frames.Last().Body[3];

		Assert.That(second.Rows[0][0].Text, Is.EqualTo("row30"), "the wheel's arrow is the same page turn");
	}

	[Test]
	public void Show_LeftArrowOnTheFirstPage_RedrawsNothing()
	{
		var harness = new Harness(2);

		harness.Keys.Press(ConsoleKey.LeftArrow, ConsoleKey.Escape);

		harness.Show();

		Assert.That(harness.Live.Frames, Has.Count.EqualTo(1), "no update when the page cannot move back");
	}

	[Test]
	public void Show_TypingFiltersTheRowsAndBackspaceGrowsThemBack()
	{
		var harness = new Harness(45);

		foreach (var letter in "value44")
			harness.Keys.Press(letter);

		for (var i = 0; i < 6; i++)
			harness.Keys.Press(ConsoleKey.Backspace);

		harness.Keys.Press(ConsoleKey.Escape);

		harness.Show();

		var narrowed = (TableBlock)harness.Live.Frames[7].Body[3];
		var query = (TextBlock)harness.Live.Frames[7].Body[1];
		var widened = (TableBlock)harness.Live.Frames[13].Body[3];
		var restored = (TextBlock)harness.Live.Frames[13].Body[5];

		Assert.Multiple(() =>
		{
			Assert.That(narrowed.Rows, Has.Count.EqualTo(1), "value44 matches a single row");
			Assert.That(narrowed.Rows[0][0].Text, Is.EqualTo("row44"));
			Assert.That(LineText.Of(query.Lines.Single()), Does.Contain("value44"), "the filter band shows the query");
			Assert.That(widened.Rows, Has.Count.EqualTo(30), "six backspaces restored 'value' and the first page fills up again");
			Assert.That(LineText.Of(restored.Lines.Single()), Does.Contain("45 total users"), "the count is back to every row");
		});
	}

	[Test]
	public void Show_WithoutRows_WritesTheEmptyNotice()
	{
		var harness = new Harness(0);

		harness.Keys.Press(ConsoleKey.Escape);

		harness.Show();

		var notice = (TextBlock)harness.Live.Frames.Single().Body[3];

		Assert.Multiple(() =>
		{
			Assert.That(LineText.Of(notice.Lines.Single()), Is.EqualTo("No users found."));
			Assert.That(notice.Lines.Single().Single().Role, Is.EqualTo(TextRole.Muted));
		});
	}

	private sealed class Harness
	{
		public readonly FakeKeyReader Keys = new();
		public readonly FakeScreenCanvas Canvas = new();
		public readonly FakeLiveFrame Live = new();
		public readonly IReadOnlyList<StyledText> Headers =
		[
			new StyledText("User", TextRole.Muted),
			new StyledText("Role", TextRole.Muted)
		];
		public readonly IReadOnlyList<IReadOnlyList<StyledText>> Rows;
		public readonly ListBrowser Browser;

		public Harness(int rowCount)
		{
			var localization = new EnglishLocalization();
			var settings = new AppSettingsStore(new FakeSettingsRepository());
			var statusBar = new StatusBar(new StubAppInfo(), new ConnectionSession(), localization);

			Rows = [.. Enumerable.Range(0, rowCount).Select(IndexedRow)];
			Browser = new ListBrowser(new Screen(Canvas, new Header(), statusBar), new Header(), new BrowseLayout(localization), Live, Keys, settings);
		}

		public void Show() => Browser.Show(Headers, Rows, "No users found.", "total users");

		private static IReadOnlyList<StyledText> IndexedRow(int index) =>
		[
			new StyledText($"row{index}", TextRole.Primary),
			new StyledText($"value{index}", TextRole.Primary)
		];

		private sealed class StubAppInfo : IAppInfo
		{
			public string Version => "0.0";
		}
	}
}
