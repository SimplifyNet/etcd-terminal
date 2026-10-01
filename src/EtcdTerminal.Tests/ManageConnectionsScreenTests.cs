using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Presentation.Terminal;
using EtcdTerminal.Presentation;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ManageConnectionsScreenTests
{
	[Test]
	public void Show_Escape_PresentsTheTitleAndEveryActionInOneFrame()
	{
		var harness = new Harness(Config("prod"), Config("staging"));

		harness.Terminal.Press(ConsoleKey.Escape);

		harness.Screen.Show(harness.Instances);

		var frame = harness.Host.Frames.Single();

		Assert.That(frame.Body, Has.Count.EqualTo(2));
		Assert.That(frame.Body[0].Kind, Is.EqualTo(PanelKind.Title));
		Assert.That(frame.Body[0].Lines.Single().Text, Is.EqualTo(harness.Terminal.Indent + "Manage Connections"));

		var actions = frame.Body[1].Lines.Select(line => line.Text).ToList();

		Assert.That(actions, Has.Count.EqualTo(5));
		Assert.That(actions[0], Does.Contain("Add Instance"));
		Assert.That(actions[1], Does.Contain("Edit Instance"));
		Assert.That(actions[2], Does.Contain("Remove Instance"));
		Assert.That(harness.Host.BeginCount, Is.EqualTo(harness.Host.EndCount), "the frame is released even when cancelled");
	}

	[Test]
	public void Show_WithoutInstances_OffersOnlyAdding()
	{
		var harness = new Harness();

		harness.Terminal.Press(ConsoleKey.Escape);

		harness.Screen.Show(harness.Instances);

		var actions = harness.Host.Frames.Single().Body[1].Lines.Select(line => line.Text).ToList();

		Assert.That(actions, Has.Count.EqualTo(1));
	}

	[Test]
	public void RemoveInstance_Confirmed_RemovesItAndReleasesEveryFrame()
	{
		var harness = new Harness(Config("prod"), Config("staging"));

		harness.Terminal.Press(ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.Enter, ConsoleKey.Enter, ConsoleKey.Enter);

		harness.Screen.Show(harness.Instances);

		Assert.That(harness.Repository.Removed, Is.EqualTo("prod"));
		Assert.That(harness.Host.BeginCount, Is.EqualTo(2), "the action menu and the instance picker own a frame, the outcome streams below them");
		Assert.That(harness.Host.EndCount, Is.EqualTo(harness.Host.BeginCount));
		Assert.That(harness.Renderer.ClearRequests, Is.EqualTo(1), "the outcome clears the frame rows it streams over");
	}

	private static EtcdConnectionConfig Config(string name) => new()
	{
		Name = name,
		ConnectionString = "http://localhost:2379"
	};

	private sealed class Harness
	{
		public readonly FakeTerminal Terminal = new();
		public readonly FakeScreenHost Host = new();
		public readonly IReadOnlyList<EtcdConnectionConfig> Instances;
		public readonly RecordingConfigRepository Repository;
		public readonly FakeStatusBarRenderer Renderer = new();
		public readonly ManageConnectionsScreen Screen;

		public Harness(params EtcdConnectionConfig[] instances)
		{
			Instances = instances;
			Repository = new RecordingConfigRepository(instances);

			var localization = new EnglishLocalization();
			var statusBar = new StatusBar(Terminal, new StubAppInfo(), new ConnectionSession(), localization, Renderer);
			var prompt = new Prompt(Terminal, Terminal, Terminal, new StubTextInput(), statusBar);
			var message = new Message(Terminal, statusBar, localization);
			var menu = new Menu(Terminal, Terminal, Host, new Header(), statusBar);

			Screen = new ManageConnectionsScreen(Terminal, Repository, menu, prompt, message, localization, new AppSettingsStore());
		}
	}

	private sealed class RecordingConfigRepository(IReadOnlyList<EtcdConnectionConfig> instances) : IConnectionConfigRepository
	{
		public string? Removed { get; private set; }

		public IReadOnlyList<EtcdConnectionConfig> LoadInstances() => instances;

		public void AddInstance(EtcdConnectionConfig config) => throw new NotSupportedException();

		public void UpdateInstance(string originalName, EtcdConnectionConfig config) => throw new NotSupportedException();

		public void RemoveInstance(string name) => Removed = name;

		public void MoveUp(string name) => throw new NotSupportedException();

		public void MoveDown(string name) => throw new NotSupportedException();
	}

	private sealed class StubTextInput : ITextInput
	{
		public string? ReadLine(string prompt, string? defaultValue = null) => defaultValue;

		public string? ReadSecret(string prompt) => null;
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
