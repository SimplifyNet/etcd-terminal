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
public sealed class ConnectionWorkflowTests
{
	[Test]
	public async Task SuccessfulConnect_StartsSessionOnce()
	{
		var harness = new Harness([Config()], (_, _) => Task.FromResult(UserCapabilities.Unrestricted));

		harness.Answers.Answer(new InstanceMenuChoice(null, harness.Instances[0]));

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
			Task.FromException<UserCapabilities>(new EtcdOperationException(EtcdOperationFailureKind.Unavailable, "unreachable")));

		harness.Answers.Answer(new InstanceMenuChoice(null, harness.Instances[0]));
		harness.Answers.Cancel();
		harness.Keys.Press(ConsoleKey.Enter);

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
		});

		harness.Answers.Answer(new InstanceMenuChoice(null, harness.Instances[0]));
		harness.Answers.Cancel();
		harness.Keys.Press(ConsoleKey.Escape, ConsoleKey.Enter);

		var selected = await harness.Selection.ShowAsync();

		Assert.That(selected, Is.Null);
		Assert.That(harness.Session.Active, Is.Null);
		Assert.That(harness.Connection.ConnectCalls, Is.EqualTo(1));
		Assert.That(harness.Connection.DisconnectCalls, Is.EqualTo(1));
		Assert.That(ComposedText(harness), Does.Contain("Operation cancelled."));
	}

	/// The text every screen streamed, in order. An outcome is written to the
	/// canvas below the prompt it follows, so it is asserted there.
	private static string ComposedText(Harness harness) =>
		string.Join('\n', harness.Canvas.Blocks.SelectMany(BlockLines));

	private static IEnumerable<string> BlockLines(Block block) =>
		block is TextBlock text ? text.Lines.Select(LineText.Of) : [];

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
		public readonly InstanceSelectionScreen Selection;

		public Harness(
			IReadOnlyList<EtcdConnectionConfig> instances,
			Func<string?, CancellationToken, Task<UserCapabilities>> discover)
		{
			Instances = instances;
			StatusBar = new StatusBar(new StubAppInfo(), Session, Localization);

			var header = new Header();
			var screen = new Screen(Canvas, header, StatusBar);

			Menu = new Menu(screen, Answers);

			var message = new Message(screen, Keys, Localization);
			var prompt = new Prompt(new StubTextInput());
			var spinner = new Spinner(Keys, new FakeStatusIndicator());
			var manage = new ManageConnectionsScreen(new StubConfigRepo(instances), Menu, prompt, message, Localization, new AppSettingsStore());
			var settings = new SettingsScreen(new StubSettingsRepo(), Menu, prompt, message, Localization, new AppSettingsStore());
			var workflow = new ConnectionWorkflow(Connection, new StubCapabilities(discover), Session);

			Selection = new InstanceSelectionScreen(new StubConfigRepo(instances), new StubDecryptSource(), workflow, settings, Menu, message, spinner, manage, Localization);
		}
	}

	private sealed class StubConnection : IEtcdConnection
	{
		public int ConnectCalls;
		public int DisconnectCalls;

		public Task ConnectAsync(EtcdConnectionConfig config, CancellationToken ct = default)
		{
			ConnectCalls++;

			return Task.CompletedTask;
		}

		public Task DisconnectAsync()
		{
			DisconnectCalls++;

			return Task.CompletedTask;
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
}
