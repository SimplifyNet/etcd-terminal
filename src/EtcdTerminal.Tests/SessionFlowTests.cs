using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Security;
using EtcdTerminal.Session;
using EtcdTerminal.Terminal;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class SessionFlowTests
{
	[Test]
	public async Task EscapeDuringMenu_ClearsSessionAndDisconnects()
	{
		var harness = new Harness([], (_, _) => Task.FromResult(UserCapabilities.Unrestricted), []);

		harness.Session.Start(Config(), UserCapabilities.Unrestricted);
		harness.Terminal.Press(ConsoleKey.Escape);

		await harness.Main.ShowAsync();

		Assert.That(harness.Session.Active, Is.Null);
		Assert.That(harness.Connection.DisconnectCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task DisconnectMenuItem_ClearsSessionAndDisconnects()
	{
		var harness = new Harness([], (_, _) => Task.FromResult(UserCapabilities.Unrestricted),
		[
			new StubEntry(MainMenuAction.BrowseKeys)
		]);

		harness.Session.Start(Config(), UserCapabilities.Unrestricted);
		harness.Terminal.Press(ConsoleKey.DownArrow, ConsoleKey.Enter);

		await harness.Main.ShowAsync();

		Assert.That(harness.Session.Active, Is.Null);
		Assert.That(harness.Connection.DisconnectCalls, Is.EqualTo(1));
	}

	[Test]
	public void ThrowingEntry_CleansUpAndPropagates()
	{
		var harness = new Harness([], (_, _) => Task.FromResult(UserCapabilities.Unrestricted),
		[
			new StubEntry(MainMenuAction.BrowseKeys, () => Task.FromException(new InvalidOperationException("boom")))
		]);

		harness.Session.Start(Config(), UserCapabilities.Unrestricted);
		harness.Terminal.Press(ConsoleKey.Enter);

		var ex = Assert.ThrowsAsync<InvalidOperationException>(() => harness.Main.ShowAsync());

		Assert.That(ex!.Message, Is.EqualTo("boom"));
		Assert.That(harness.Session.Active, Is.Null);
		Assert.That(harness.Connection.DisconnectCalls, Is.EqualTo(1));
	}

	[Test]
	public void ThrowingEntry_FailingDisconnect_OriginalErrorSurfaces()
	{
		var harness = new Harness([], (_, _) => Task.FromResult(UserCapabilities.Unrestricted),
		[
			new StubEntry(MainMenuAction.BrowseKeys, () => Task.FromException(new InvalidOperationException("boom")))
		]);

		harness.Session.Start(Config(), UserCapabilities.Unrestricted);
		harness.Connection.OnDisconnect = () => Task.FromException(new InvalidOperationException("disconnect boom"));
		harness.Terminal.Press(ConsoleKey.Enter);

		var ex = Assert.ThrowsAsync<InvalidOperationException>(() => harness.Main.ShowAsync());

		Assert.That(ex!.Message, Is.EqualTo("boom"));
		Assert.That(harness.Session.Active, Is.Null);
	}

	[Test]
	public void NormalExit_FailingDisconnect_SurfacesAndClearsSession()
	{
		var harness = new Harness([], (_, _) => Task.FromResult(UserCapabilities.Unrestricted), []);

		harness.Session.Start(Config(), UserCapabilities.Unrestricted);
		harness.Connection.OnDisconnect = () => Task.FromException(new InvalidOperationException("disconnect boom"));
		harness.Terminal.Press(ConsoleKey.Escape);

		var ex = Assert.ThrowsAsync<InvalidOperationException>(() => harness.Main.ShowAsync());

		Assert.That(ex!.Message, Is.EqualTo("disconnect boom"));
		Assert.That(harness.Session.Active, Is.Null);
	}

	[Test]
	public async Task SuccessfulConnect_StartsSessionOnce()
	{
		var harness = new Harness([Config()], (_, _) => Task.FromResult(UserCapabilities.Unrestricted), []);

		harness.Terminal.Press(ConsoleKey.Enter);

		var selected = await harness.Selection.ShowAsync();

		Assert.That(selected?.Name, Is.EqualTo("prod"));
		Assert.That(harness.Session.Active?.Name, Is.EqualTo("prod"));
		Assert.That(harness.Connection.ConnectCalls, Is.EqualTo(1));
		Assert.That(harness.Connection.DisconnectCalls, Is.EqualTo(0));
	}

	[Test]
	public async Task DiscoveryFailure_DisconnectsClearsAndReturnsToSelection()
	{
		var harness = new Harness([Config()], (_, _) =>
			Task.FromException<UserCapabilities>(new EtcdOperationException(EtcdOperationFailureKind.Unavailable, "unreachable")), []);

		harness.Terminal.Press(ConsoleKey.Enter, ConsoleKey.Enter, ConsoleKey.Escape);

		var selected = await harness.Selection.ShowAsync();

		Assert.That(selected, Is.Null);
		Assert.That(harness.Session.Active, Is.Null);
		Assert.That(harness.Connection.ConnectCalls, Is.EqualTo(1));
		Assert.That(harness.Connection.DisconnectCalls, Is.EqualTo(1));
		Assert.That(ComposedText(harness), Does.Contain("Failed to connect"));
	}

	[Test]
	public async Task DiscoveryCancellation_DisconnectsAndReportsCancelled()
	{
		var harness = new Harness([Config()], async (_, ct) =>
		{
			await Task.Delay(500, ct);

			return UserCapabilities.Unrestricted;
		}, []);

		harness.Terminal.Press(ConsoleKey.Enter, ConsoleKey.Escape, ConsoleKey.Enter, ConsoleKey.Escape);

		var selected = await harness.Selection.ShowAsync();

		Assert.That(selected, Is.Null);
		Assert.That(harness.Session.Active, Is.Null);
		Assert.That(harness.Connection.ConnectCalls, Is.EqualTo(1));
		Assert.That(harness.Connection.DisconnectCalls, Is.EqualTo(1));
		Assert.That(ComposedText(harness), Does.Contain("Operation cancelled."));
	}

	[Test]
	public async Task ReopenedSelection_ShowsNoPreviousConnectionInFooter()
	{
		var harness = new Harness([Config()], (_, _) => Task.FromResult(UserCapabilities.Unrestricted), []);

		harness.Terminal.Press(ConsoleKey.Enter);

		var selected = await harness.Selection.ShowAsync();

		Assert.That(selected?.Name, Is.EqualTo("prod"));

		harness.Terminal.Press(ConsoleKey.Escape);

		await harness.Main.ShowAsync();

		Assert.That(harness.Session.Active, Is.Null);

		harness.Terminal.Output.Clear();
		harness.Terminal.Press(ConsoleKey.Escape);

		var second = await harness.Selection.ShowAsync();

		Assert.That(second, Is.Null);

		// The framed screen owns the footer now, so the visible model comes from
		// the host, not from the inline writer.
		var footer = harness.Host.Frames[^1].Footer;

		Assert.That(footer, Is.Not.Null);
		Assert.That(footer!.Name, Is.Null);
		Assert.That(footer.Version.Text, Is.EqualTo("0.0"));
	}

	/// The text every screen composed, in order. An outcome is a frame of its
	/// own now, so it is asserted on the host rather than on streamed output.
	private static string ComposedText(Harness harness) =>
		string.Join('\n', harness.Host.Frames.SelectMany(frame => frame.Body).SelectMany(panel => panel.Lines).Select(line => line.Text));

	private static EtcdConnectionConfig Config() => new()
	{
		Name = "prod",
		ConnectionString = "http://localhost:2379"
	};

	private sealed class Harness
	{
		public readonly FakeTerminal Terminal = new();
		public readonly ConnectionSession Session = new();
		public readonly StubConnection Connection = new();
		public readonly EnglishLocalization Localization = new();
		public readonly FakeStatusBarRenderer Footer = new();
		public readonly FakeScreenHost Host = new();
		public readonly StatusBar StatusBar;
		public readonly Menu Menu;
		public readonly MainScreen Main;
		public readonly InstanceSelectionScreen Selection;

		public Harness(
			IReadOnlyList<EtcdConnectionConfig> instances,
			Func<string?, CancellationToken, Task<UserCapabilities>> discover,
			IEnumerable<IMainMenuEntry> entries)
		{
			StatusBar = new StatusBar(Terminal, new StubAppInfo(), Session, Localization, Footer);
			var header = new Header();
			Menu = new Menu(Terminal, Terminal, Host, header, StatusBar);

			var message = new Message(Host, header, StatusBar, Terminal, Localization);
			var prompt = new Prompt(Terminal, Terminal, Terminal, new StubTextInput(), StatusBar);
			var spinner = new Spinner(Terminal, new FakeStatusIndicator());
			var manage = new ManageConnectionsScreen(Terminal, new StubConfigRepo(instances), Menu, prompt, message, Localization, new AppSettingsStore());
			var settings = new SettingsScreen(new StubSettingsRepo(), Menu, prompt, message, Localization, new AppSettingsStore());

			Selection = new InstanceSelectionScreen(new StubConfigRepo(instances), new StubDecryptSource(), Connection, Session, new StubCapabilities(discover), settings, Menu, message, spinner, manage, Localization);
			Main = new MainScreen(Connection, Session, entries, Menu, Localization);
		}
	}

	private sealed class StubConnection : IEtcdConnection
	{
		public int ConnectCalls;
		public int DisconnectCalls;
		public Func<Task>? OnDisconnect;

		public Task ConnectAsync(EtcdConnectionConfig config, CancellationToken ct = default)
		{
			ConnectCalls++;

			return Task.CompletedTask;
		}

		public async Task DisconnectAsync()
		{
			DisconnectCalls++;

			if (OnDisconnect is not null)
				await OnDisconnect();
		}
	}

	private sealed class StubCapabilities(Func<string?, CancellationToken, Task<UserCapabilities>> _discover) : IUserCapabilitiesProvider
	{
		public Task<UserCapabilities> GetCapabilitiesAsync(string? username, CancellationToken ct = default) =>
			_discover(username, ct);
	}

	private sealed class StubConfigRepo(IReadOnlyList<EtcdConnectionConfig> instances) : IConnectionConfigRepository
	{
		public IReadOnlyList<EtcdConnectionConfig> LoadInstances() => instances;

		public void AddInstance(EtcdConnectionConfig config) => throw new NotSupportedException();

		public void UpdateInstance(string originalName, EtcdConnectionConfig config) => throw new NotSupportedException();

		public void RemoveInstance(string name) => throw new NotSupportedException();

		public void MoveUp(string name) => throw new NotSupportedException();

		public void MoveDown(string name) => throw new NotSupportedException();
	}

	private sealed class StubDecryptSource : IDecryptFailureSource
	{
		public IReadOnlyList<string> TakeDecryptFailures() => [];
	}

	private sealed class StubSettingsRepo : IAppSettingsRepository
	{
		public IAppSettings Load() => throw new NotSupportedException();

		public void Save(IAppSettings settings) => throw new NotSupportedException();
	}

	private sealed class StubTextInput : ITextInput
	{
		public string? ReadLine(string prompt, string? defaultValue = null) => throw new NotSupportedException();

		public string? ReadSecret(string prompt) => throw new NotSupportedException();
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}

	private sealed class StubEntry(MainMenuAction action, Func<Task>? behavior = null) : IMainMenuEntry
	{
		public MainMenuAction Action => action;

		public string Label => action.ToString();

		public bool IsAvailable(UserCapabilities capabilities) => true;

		public Task ShowAsync() => behavior?.Invoke() ?? Task.CompletedTask;
	}
}
