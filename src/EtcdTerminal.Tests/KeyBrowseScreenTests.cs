using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens.Keys;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Keys;
using EtcdTerminal.Security;
using EtcdTerminal.Session;
using EtcdTerminal.Terminal;
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
		harness.Terminal.Keys.Enqueue(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
		harness.Terminal.Press(ConsoleKey.Enter, ConsoleKey.E, ConsoleKey.Enter, ConsoleKey.DownArrow, ConsoleKey.Enter, ConsoleKey.E, ConsoleKey.Enter, ConsoleKey.Escape);

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

		harness.Terminal.Keys.Enqueue(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
		harness.Terminal.Press(ConsoleKey.Enter, ConsoleKey.D, ConsoleKey.Enter, ConsoleKey.Escape);

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
		harness.Terminal.Keys.Enqueue(new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false));
		harness.Terminal.Press(ConsoleKey.Enter, ConsoleKey.E, ConsoleKey.Enter, ConsoleKey.DownArrow, ConsoleKey.Enter, ConsoleKey.E, ConsoleKey.Enter, ConsoleKey.Escape);

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

		harness.Terminal.Press(ConsoleKey.Escape);

		await harness.Screen.ShowAsync();

		var frame = harness.Host.Frames[0];

		Assert.That(frame.Header, Is.Not.Null);
		Assert.That(frame.Footer, Is.Not.Null);
		Assert.That(frame.Body, Has.Count.EqualTo(3));
		Assert.That(frame.Body[0].Lines.Single().Text, Does.Contain("Type to search"));
		Assert.That(frame.Body[1].Lines[0].Spans[1].Text, Does.Contain("/a/1"));
		Assert.That(frame.Body[2].Lines.Single().Text, Does.Contain("1/1"));
		Assert.That(harness.Host.EndCount, Is.EqualTo(harness.Host.BeginCount), "the screen must not leak the console");
	}

	[Test]
	public async Task SelectedKey_OffersEditAndDeleteOnlyWithWriteRights()
	{
		var harness = new Harness(new() { ["/a/1"] = "x" });

		harness.Terminal.Press(ConsoleKey.Enter, ConsoleKey.Escape, ConsoleKey.Escape);

		await harness.Screen.ShowAsync();

		var withActions = harness.Host.Frames.Last(frame => frame.Body.Count == 5);

		Assert.That(withActions.Body[3].Lines.Single().Text, Does.Contain("Selected: /a/1"));
		Assert.That(withActions.Body[4].Lines.Single().Text, Does.Contain("E Edit"));
		Assert.That(withActions.Body[4].Lines.Single().Text, Does.Contain("D Delete"));
	}

	private sealed class Harness
	{
		public readonly FakeTerminal Terminal = new();
		public readonly QueueTextInput TextInput = new();
		public readonly FakeScreenHost Host = new();
		public readonly DictKeyStore Store;
		public readonly KeyBrowseControl Control;
		public readonly KeyBrowseScreen Screen;

		public Harness(Dictionary<string, string> initial)
		{
			var localization = new EnglishLocalization();
			var session = new ConnectionSession();
			var settings = new AppSettingsStore();

			session.Start(new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://localhost:2379" }, UserCapabilities.Unrestricted);

			Store = new DictKeyStore(initial);

			var statusBar = new StatusBar(Terminal, Terminal, new StubAppInfo(), session, localization, new FakeStatusBarRenderer());
			var prompt = new Prompt(Terminal, Terminal, Terminal, TextInput, statusBar);
			var message = new Message(Host, new Header(), statusBar, Terminal, localization);
			var browseLayout = new KeyBrowseLayout(Terminal, localization);

			Control = new KeyBrowseControl(Terminal, browseLayout, Host, new Header(), statusBar, session);

			Screen = new KeyBrowseScreen(Store, new ReadableKeysProvider(Store), session, Control, browseLayout, prompt, message, localization, settings);
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
