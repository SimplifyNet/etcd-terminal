using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Terminal;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class SettingsScreenTests
{
	[Test]
	public void Show_Cancel_PresentsTheTitleAndBothActions()
	{
		var harness = new Harness();

		harness.Answers.Cancel();

		harness.Screen.Show();

		var offered = harness.Answers.Prompt<SettingsAction>(0);

		Assert.Multiple(() =>
		{
			Assert.That(offered.Title, Is.EqualTo("Settings"));
			Assert.That(offered.Items.Select(i => i.Label), Is.EqualTo(new[]
			{
				"Keys per page (30)",
				"Trim input values (On)"
			}));
			Assert.That(harness.Canvas.NewScreenCount, Is.EqualTo(1));
		});
	}

	[Test]
	public void Show_ToggleTrimInputValues_PersistsAndOffersTheMenuAgain()
	{
		var harness = new Harness();

		harness.Answers.Answer(SettingsAction.ToggleTrimInputValues);
		harness.Answers.Cancel();

		harness.Screen.Show();

		Assert.Multiple(() =>
		{
			Assert.That(harness.Settings.Current.TrimInputValues, Is.False);
			Assert.That(harness.Repository.Saved?.TrimInputValues, Is.False);
			Assert.That(harness.Answers.Prompts, Has.Count.EqualTo(2), "the menu is composed again after the action");
		});
	}

	private sealed class Harness
	{
		public readonly FakeSelectionPrompt Answers = new();
		public readonly FakeScreenCanvas Canvas = new();
		public readonly RecordingSettingsRepository Repository = new();
		public readonly AppSettingsStore Settings = new();
		public readonly SettingsScreen Screen;

		public Harness()
		{
			var localization = new EnglishLocalization();
			var statusBar = new StatusBar(new StubAppInfo(), new ConnectionSession(), localization);
			var prompt = new Prompt(new StubTextInput());
			var screen = new Screen(Canvas, new Header(), statusBar);
			var message = new Message(screen, new FakeKeyReader(), localization);
			var menu = new Menu(screen, Answers);

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
