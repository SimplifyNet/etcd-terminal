using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens.Connections;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Presentation;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ManageConnectionsScreenTests
{
	[Test]
	public void Show_Cancel_PresentsTheTitleAndEveryAction()
	{
		var harness = new Harness(Config("prod"), Config("staging"));

		harness.Answers.Cancel();

		harness.Screen.Show(harness.Instances);

		var offered = harness.Answers.Prompt<ManageConnectionsAction>(0);

		Assert.Multiple(() =>
		{
			Assert.That(offered.Title, Is.EqualTo("Manage Connections"));
			Assert.That(offered.Items.Select(i => i.Label), Is.EqualTo(new[]
			{
				"Add Instance",
				"Edit Instance",
				"Remove Instance",
				"Move Up",
				"Move Down"
			}));
			Assert.That(harness.Canvas.NewScreenCount, Is.EqualTo(1));
		});
	}

	[Test]
	public void Show_WithoutInstances_OffersOnlyAdding()
	{
		var harness = new Harness();

		harness.Answers.Cancel();

		harness.Screen.Show(harness.Instances);

		var offered = harness.Answers.Prompt<ManageConnectionsAction>(0);

		Assert.That(offered.Items, Has.Count.EqualTo(1));
	}

	[Test]
	public void RemoveInstance_Confirmed_RemovesItAndStreamsTheOutcome()
	{
		var harness = new Harness(Config("prod"), Config("staging"));

		harness.Answers.Answer(ManageConnectionsAction.RemoveInstance);
		harness.Answers.Answer("prod");
		harness.Keys.Press(ConsoleKey.Enter);

		harness.Screen.Show(harness.Instances);

		Assert.Multiple(() =>
		{
			Assert.That(harness.Repository.Removed, Is.EqualTo("prod"));
			Assert.That(harness.Answers.Prompts, Has.Count.EqualTo(2), "the action menu and the instance picker");
			Assert.That(harness.Canvas.NewScreenCount, Is.EqualTo(2), "every menu opens the screen it prompts on");
			Assert.That(CanvasText(harness), Does.Contain("Instance removed successfully!"), "the outcome streams below the prompt");
		});
	}

	[Test]
	public void AddInstance_DuplicateName_ShowsErrorWithoutSaving()
	{
		var harness = new Harness(Config("prod"), Config("staging"));

		harness.Answers.Answer(ManageConnectionsAction.AddInstance);
		harness.TextInput.Enqueue("staging");
		harness.Keys.Press(ConsoleKey.Enter);

		harness.Screen.Show(harness.Instances);

		Assert.Multiple(() =>
		{
			Assert.That(harness.Answers.Prompts, Has.Count.EqualTo(1), "the action menu is the only selection");
			Assert.That(CanvasText(harness), Does.Contain("An instance with this name already exists."));
		});
	}

	private static string CanvasText(Harness harness) =>
		string.Join('\n', harness.Canvas.Blocks.OfType<TextBlock>().SelectMany(block => block.Lines.Select(LineText.Of)));

	private static EtcdConnectionConfig Config(string name) => new()
	{
		Name = name,
		ConnectionString = "http://localhost:2379"
	};

	private sealed class Harness
	{
		public readonly FakeKeyReader Keys = new();
		public readonly FakeSelectionPrompt Answers = new();
		public readonly StubTextInput TextInput = new();
		public readonly IReadOnlyList<EtcdConnectionConfig> Instances;
		public readonly RecordingConfigRepository Repository;
		public readonly FakeScreenCanvas Canvas = new();
		public readonly ManageConnectionsScreen Screen;

		public Harness(params EtcdConnectionConfig[] instances)
		{
			Instances = instances;
			Repository = new RecordingConfigRepository(instances);

			var keys = Keys;
			LocalizationCatalog localization = new();
			var statusBar = new StatusBar(new StubAppInfo(), new ConnectionSession(), localization);
			var prompt = new Prompt(TextInput);
			var screen = new Screen(Canvas, new Header(), statusBar);
			var message = new Message(screen, keys, localization);
			var menu = new Menu(screen, Answers);
			var userInput = new UserInput(prompt, new AppSettingsStore(new FakeSettingsRepository()));

			Screen = new ManageConnectionsScreen(menu, new ConnectionEditor(Repository, userInput, message, localization), new ConnectionOrganizer(Repository, menu, message, localization), localization);
		}
	}

	private sealed class RecordingConfigRepository(IReadOnlyList<EtcdConnectionConfig> instances) : IConnectionConfigRepository
	{
		public string? Removed { get; private set; }

		public IReadOnlyList<EtcdConnectionConfig> LoadInstances() => instances;

		public bool IsNameTaken(string name, string? exceptName) =>
			instances.Any(i => i.Name == name && i.Name != exceptName);

		public void AddInstance(EtcdConnectionConfig config) => throw new NotSupportedException();

		public void UpdateInstance(string originalName, EtcdConnectionConfig config) => throw new NotSupportedException();

		public void RemoveInstance(string name) => Removed = name;

		public void MoveUp(string name) => throw new NotSupportedException();

		public void MoveDown(string name) => throw new NotSupportedException();
	}

	private sealed class StubTextInput : ITextInput
	{
		private readonly Queue<string?> _lines = new();

		public void Enqueue(string line) => _lines.Enqueue(line);

		public string? ReadLine(string prompt, string? defaultValue = null) =>
			_lines.Count > 0 ? _lines.Dequeue() : defaultValue;

		public string? ReadSecret(string prompt) => null;
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
