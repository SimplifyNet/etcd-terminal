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

	private sealed class Harness
	{
		public readonly FakeTerminal Terminal = new();
		public readonly QueueTextInput TextInput = new();
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

			var statusBar = new StatusBar(Terminal, Terminal, Terminal, new StubAppInfo(), session, localization);
			var layout = new ScreenLayout(Terminal, Terminal, statusBar, new Header(Terminal));
			var prompt = new Prompt(Terminal, Terminal, Terminal, TextInput, statusBar);
			var pressAnyKey = new PressAnyKeyPrompt(Terminal, Terminal, statusBar, localization);
			var message = new Message(Terminal, pressAnyKey);
			var browseLayout = new KeyBrowseLayout(Terminal, Terminal, Terminal, localization);

			Control = new KeyBrowseControl(Terminal, Terminal, Terminal, statusBar, browseLayout, layout, session);

			Screen = new KeyBrowseScreen(Terminal, Store, new ReadableKeysProvider(Store), session, layout, Control, prompt, message, localization, settings);
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
