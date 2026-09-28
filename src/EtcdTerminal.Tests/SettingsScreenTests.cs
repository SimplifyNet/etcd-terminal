using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Session;
using EtcdTerminal.Terminal;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class SettingsScreenTests
{
	[Test]
	public void Show_Escape_PresentsTheTitleAndBothActionsInOneFrame()
	{
		var harness = new Harness();

		harness.Terminal.Press(ConsoleKey.Escape);

		harness.Screen.Show();

		var frame = harness.Host.Frames.Single();

		var actions = frame.Body[1].Lines.Select(line => line.Text).ToList();

		Assert.That(frame.Body, Has.Count.EqualTo(2));
		Assert.That(frame.Body[0].Lines.Single().Text, Is.EqualTo("Settings"));
		Assert.That(actions, Has.Count.EqualTo(2));
		Assert.That(actions[0], Does.Contain("Keys per page (30)"));
		Assert.That(actions[1], Does.Contain("Trim input values (On)"));
		Assert.That(harness.Host.BeginCount, Is.EqualTo(harness.Host.EndCount), "the frame is released even when cancelled");
	}

	[Test]
	public void Show_ToggleTrimInputValues_PersistsAndOffersTheMenuAgain()
	{
		var harness = new Harness();

		harness.Terminal.Press(ConsoleKey.DownArrow, ConsoleKey.Enter, ConsoleKey.Escape);

		harness.Screen.Show();

		Assert.That(harness.Settings.Current.TrimInputValues, Is.False);
		Assert.That(harness.Repository.Saved?.TrimInputValues, Is.False);
		Assert.That(harness.Host.BeginCount, Is.EqualTo(2), "the menu is composed again after the action");
		Assert.That(harness.Host.EndCount, Is.EqualTo(harness.Host.BeginCount));
	}

	private sealed class Harness
	{
		public readonly FakeTerminal Terminal = new();
		public readonly FakeScreenHost Host = new();
		public readonly RecordingSettingsRepository Repository = new();
		public readonly AppSettingsStore Settings = new();
		public readonly SettingsScreen Screen;

		public Harness()
		{
			var localization = new EnglishLocalization();
			var statusBar = new StatusBar(Terminal, Terminal, new StubAppInfo(), new ConnectionSession(), localization, new FakeStatusBarRenderer());
			var prompt = new Prompt(Terminal, Terminal, Terminal, new StubTextInput(), statusBar);
			var pressAnyKey = new PressAnyKeyPrompt(Terminal, Terminal, statusBar, localization);
			var message = new Message(Terminal, pressAnyKey);
			var menu = new Menu(Terminal, Terminal, Host, new Header(Terminal), statusBar);

			Screen = new SettingsScreen(Repository, menu, prompt, message, localization, Settings);
		}
	}

	private sealed class RecordingSettingsRepository : IAppSettingsRepository
	{
		public IAppSettings? Saved { get; private set; }

		public IAppSettings Load() => Saved ?? new AppSettings();

		public void Save(IAppSettings settings) => Saved = settings;
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
