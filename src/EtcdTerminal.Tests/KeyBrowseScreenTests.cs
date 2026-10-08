using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens.Keys;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation;
using EtcdTerminal.Security;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class KeyBrowseScreenTests
{
	[Test]
	public async Task SearchThenEdit_KeepsQueryAndSelection()
	{
		var harness = new Harness(new() { ["/a/1"] = "x", ["/b/1"] = "y" });

		harness.TextInput.Answers.Enqueue("x2");
		harness.TextInput.Answers.Enqueue("z");
		harness.Keys.Keys.Enqueue(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
		harness.Keys.Press(ConsoleKey.Enter, ConsoleKey.E, ConsoleKey.Enter, ConsoleKey.DownArrow, ConsoleKey.Enter, ConsoleKey.E, ConsoleKey.Enter, ConsoleKey.Escape);

		await harness.Screen.ShowAsync();

		Assert.That(harness.Store.Values["/a/1"], Is.EqualTo("z"));
		Assert.That(harness.Store.Values["/b/1"], Is.EqualTo("y"));
		Assert.That(harness.Control.SearchQuery, Is.EqualTo("a"));
		Assert.That(harness.Control.CurrentPage, Is.EqualTo(0));
		Assert.That(harness.Control.SelectedIndex, Is.EqualTo(0));
	}

	[Test]
	public async Task DeleteLastMatch_KeepsQueryWithValidNavigation()
	{
		var harness = new Harness(new() { ["/a/1"] = "v" });

		harness.Keys.Keys.Enqueue(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
		harness.Keys.Press(ConsoleKey.Enter, ConsoleKey.D, ConsoleKey.Enter, ConsoleKey.Escape);

		await harness.Screen.ShowAsync();

		Assert.That(harness.Store.Values, Is.Empty);
		Assert.That(harness.Control.SearchQuery, Is.EqualTo("a"));
		Assert.That(harness.Control.CurrentPage, Is.EqualTo(0));
		Assert.That(harness.Control.SelectedIndex, Is.EqualTo(0));
	}

	[Test]
	public async Task EditValueOutOfFilter_UpdatesCountsAndKeepsQuery()
	{
		var harness = new Harness(new() { ["/a/1"] = "x", ["/b/1"] = "y" });

		harness.TextInput.Answers.Enqueue("y");
		harness.TextInput.Answers.Enqueue("w");
		harness.Keys.Keys.Enqueue(new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false));
		harness.Keys.Press(ConsoleKey.Enter, ConsoleKey.E, ConsoleKey.Enter, ConsoleKey.DownArrow, ConsoleKey.Enter, ConsoleKey.E, ConsoleKey.Enter, ConsoleKey.Escape);

		await harness.Screen.ShowAsync();

		Assert.That(harness.Store.Values["/a/1"], Is.EqualTo("y"));
		Assert.That(harness.Store.Values["/b/1"], Is.EqualTo("y"));
		Assert.That(harness.Control.SearchQuery, Is.EqualTo("x"));
		Assert.That(harness.Control.CurrentPage, Is.EqualTo(0));
		Assert.That(harness.Control.SelectedIndex, Is.EqualTo(0));
	}

	[Test]
	public async Task BrowseFrame_CarriesHeaderBodyAndFooter()
	{
		var harness = new Harness(new() { ["/a/1"] = "x", ["/b/1"] = "y" });

		harness.Keys.Press(ConsoleKey.Escape);

		await harness.Screen.ShowAsync();

		var frame = harness.Live.Frames[0];

		Assert.That(frame.Body[0], Is.InstanceOf<BannerBlock>());
		Assert.That(frame.Body, Has.Count.EqualTo(6));
		Assert.That(LineText.Of(((TextBlock)frame.Body[1]).Lines.Single()), Does.Contain("Type to search"));
		Assert.That(((TextBlock)frame.Body[1]).Band, Is.True, "the filter line is a band");
		Assert.That(((TextBlock)frame.Body[2]).Lines.Single(), Is.Empty, "one blank row between the filter and the table");
		Assert.That(((TableBlock)frame.Body[3]).Rows[0][0].Text, Is.EqualTo("\u276f "), "the pointer on the selected row");
		Assert.That(((TableBlock)frame.Body[3]).Rows[0][1].Text, Does.Contain("/a/1"));
		Assert.That(((TextBlock)frame.Body[4]).Lines.Single(), Is.Empty, "one blank row between the table and the pagination");
		Assert.That(LineText.Of(((TextBlock)frame.Body[5]).Lines.Single()), Does.Contain("1/1"));
		Assert.That(((TextBlock)frame.Body[5]).Band, Is.True, "the pagination line is a band");
		Assert.That(harness.Live.Ends, Is.EqualTo(new[] { LiveFrameEnd.Clear }), "the released frame clears its viewport");
		Assert.That(harness.Canvas.Footers, Has.Count.EqualTo(1), "the footer is pinned once per frame");
	}

	[Test]
	public async Task SelectedKey_OffersEditAndDeleteOnlyWithWriteRights()
	{
		var harness = new Harness(new() { ["/a/1"] = "x" });

		harness.Keys.Press(ConsoleKey.Enter, ConsoleKey.Escape, ConsoleKey.Escape);

		await harness.Screen.ShowAsync();

		var withActions = harness.Live.Frames.Last(frame => frame.Body.Count == 8);

		var panel = (ActionPanelBlock)withActions.Body[7];

		Assert.That(withActions.Body[5], Is.InstanceOf<TextBlock>());
		Assert.That(((TextBlock)withActions.Body[5]).Band, Is.True, "the panel follows the pagination band");
		Assert.That(((TextBlock)withActions.Body[6]).Lines.Single(), Is.Empty, "a blank row separates the panel from pagination");
		Assert.That(LineText.Of(panel.Title), Does.Contain("Selected: /a/1"));
		Assert.That(LineText.Of(panel.Actions), Does.Contain("E Edit"));
		Assert.That(LineText.Of(panel.Actions), Does.Contain("D Delete"));
	}

	[Test]
	public async Task ActionPanel_HidesTheSearchCaretAndBringsItBackOnEscape()
	{
		var harness = new Harness(new() { ["/a/1"] = "x" });

		harness.Keys.Press(ConsoleKey.Enter, ConsoleKey.Escape, ConsoleKey.Escape);

		await harness.Screen.ShowAsync();

		var idle = (TextBlock)harness.Live.Frames[0].Body[1];
		var panel = (TextBlock)harness.Live.Frames[1].Body[1];
		var restored = (TextBlock)harness.Live.Frames[2].Body[1];

		Assert.That(LineText.Of(idle.Lines.Single()), Does.EndWith("\u2588"));
		Assert.That(LineText.Of(panel.Lines.Single()), Does.Not.EndWith("\u2588"), "the open panel takes the typing, so the search line loses its caret");
		Assert.That(LineText.Of(restored.Lines.Single()), Does.EndWith("\u2588"), "leaving the panel returns the caret to the search line");
	}

	private sealed class Harness
	{
		public readonly FakeKeyReader Keys = new();
		public readonly QueueTextInput TextInput = new();
		public readonly FakeScreenCanvas Canvas = new();
		public readonly FakeLiveFrame Live = new();
		public readonly DictKeyStore Store;
		public readonly KeyBrowseControl Control;
		public readonly KeyBrowseScreen Screen;

		public Harness(Dictionary<string, string> initial)
		{
			var localization = new EnglishLocalization();
			var session = new ConnectionSession();
			var settings = new AppSettingsStore(new FakeSettingsRepository());

			session.Start(new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://localhost:2379" }, UserCapabilities.Unrestricted);

			Store = new DictKeyStore(initial);

			var keys = Keys;
			var statusBar = new StatusBar(new StubAppInfo(), session, localization);
			var prompt = new Prompt(TextInput);
			var screen = new Screen(Canvas, new Header(), statusBar);
			var message = new Message(screen, keys, localization);
			var browseLayout = new KeyBrowseLayout(localization);

			Control = new KeyBrowseControl(keys, browseLayout, new BrowseLayout(localization), new Header(), session, localization);

			Screen = new KeyBrowseScreen(Store, new ReadableKeysProvider(Store), session, Control, browseLayout, new UserInput(prompt, settings), message, localization, settings, screen, Live);
		}
	}

	private sealed class DictKeyStore(Dictionary<string, string> values) : IEtcdKeyStore
	{
		public Dictionary<string, string> Values { get; } = values;

		public Task<EtcdKeyValue?> GetKeyAsync(string key, CancellationToken ct = default) =>
			Task.FromResult(Values.TryGetValue(key, out var value) ? new EtcdKeyValue { Key = key, Value = value } : null);

		public Task<IReadOnlyList<EtcdKeyValue>> GetKeysByPrefixAsync(string prefix, CancellationToken ct = default) =>
			Task.FromResult((IReadOnlyList<EtcdKeyValue>)[.. Values.Select(kv => new EtcdKeyValue { Key = kv.Key, Value = kv.Value })]);

		public Task<IReadOnlyList<EtcdKeyValue>> GetKeysByRangeAsync(string start, string endExclusive, CancellationToken ct = default) => throw new NotSupportedException();

		public Task<bool> CreateKeyAsync(string key, string value, CancellationToken ct = default)
		{
			Values[key] = value;

			return Task.FromResult(true);
		}

		public Task<bool> UpdateKeyAsync(string key, string value, CancellationToken ct = default)
		{
			Values[key] = value;

			return Task.FromResult(true);
		}

		public Task<bool> DeleteKeyAsync(string key, CancellationToken ct = default) =>
			Task.FromResult(Values.Remove(key));
	}

	private sealed class QueueTextInput : ITextInput
	{
		public readonly Queue<string?> Answers = new();

		public string? ReadLine(string prompt, string? defaultValue = null) => Answers.Dequeue();

		public string? ReadSecret(string prompt) => Answers.Dequeue();
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
