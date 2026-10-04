using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Presentation;
using EtcdTerminal.Security;
using EtcdTerminal.Session;
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
		harness.Answers.Cancel();

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
		harness.Answers.Answer(MainMenuAction.Disconnect);

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
		harness.Answers.Answer(MainMenuAction.BrowseKeys);

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
		harness.Answers.Answer(MainMenuAction.BrowseKeys);

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
		harness.Answers.Cancel();

		var ex = Assert.ThrowsAsync<InvalidOperationException>(() => harness.Main.ShowAsync());

		Assert.That(ex!.Message, Is.EqualTo("disconnect boom"));
		Assert.That(harness.Session.Active, Is.Null);
	}

	[Test]
	public async Task ReopenedSelection_ShowsNoPreviousConnectionInFooter()
	{
		var harness = new Harness([Config()], (_, _) => Task.FromResult(UserCapabilities.Unrestricted), []);

		harness.Answers.Answer(new InstanceMenuChoice(null, harness.Instances[0]));

		var selected = await harness.Selection.ShowAsync();

		Assert.That(selected?.Name, Is.EqualTo("prod"));

		harness.Answers.Cancel();

		await harness.Main.ShowAsync();

		Assert.That(harness.Session.Active, Is.Null);

		harness.Answers.Cancel();

		var second = await harness.Selection.ShowAsync();

		Assert.That(second, Is.Null);

		// The screen owns the footer, so the visible model comes from the canvas.
		var footer = harness.Canvas.Footers[^1];

		Assert.That(footer, Is.Not.Null);
		Assert.That(footer!.Name, Is.Null);
		Assert.That(footer.Version.Text, Is.EqualTo("0.0"));
	}

	private static EtcdConnectionConfig Config() => new()
	{
		Name = "prod",
		ConnectionString = "http://localhost:2379"
	};

	private sealed class Harness
	{
		public readonly ConnectionSession Session = new();
		public readonly StubConnection Connection = new();
		public readonly EnglishLocalization Localization = new();
		public readonly FakeSelectionPrompt Answers = new();
		public readonly FakeScreenCanvas Canvas = new();
		public readonly FakeKeyReader Keys = new();
		public readonly StatusBar StatusBar;
		public readonly IReadOnlyList<EtcdConnectionConfig> Instances;
		public readonly Menu Menu;
		public readonly MainScreen Main;
		public readonly InstanceSelectionScreen Selection;

		public Harness(
			IReadOnlyList<EtcdConnectionConfig> instances,
			Func<string?, CancellationToken, Task<UserCapabilities>> discover,
			IEnumerable<IMainMenuEntry> entries)
		{
			Instances = instances;
			StatusBar = new StatusBar(new StubAppInfo(), Session, Localization);

			var header = new Header();
			var screen = new Screen(Canvas, header, StatusBar);

			Menu = new Menu(screen, Answers);

			var message = new Message(screen, Keys, Localization);
			var prompt = new Prompt(new StubTextInput());
			var spinner = new Spinner(Keys, new FakeStatusIndicator());
			var manage = new ManageConnectionsScreen(new StubConfigRepo(instances), Menu, prompt, message, Localization, new AppSettingsStore(new FakeSettingsRepository()));
			var settings = new SettingsScreen(Menu, prompt, message, Localization, new AppSettingsStore(new FakeSettingsRepository()));
			var workflow = new ConnectionWorkflow(Connection, new StubCapabilities(discover), Session);

			Selection = new InstanceSelectionScreen(new StubConfigRepo(instances), new StubDecryptSource(), workflow, settings, Menu, message, spinner, manage, Localization);
			Main = new MainScreen(workflow, Session, entries, Menu, Localization);
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

		public bool IsNameTaken(string name, string? exceptName) => throw new NotSupportedException();

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
