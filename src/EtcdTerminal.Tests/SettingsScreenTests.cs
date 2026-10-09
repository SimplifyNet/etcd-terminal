using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens.Settings;
using EtcdTerminal.App.Setup;
using EtcdTerminal.App.Theming;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Presentation;
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
				"Language (English)",
				"Theme (Reddy (Black))",
				"Items per page (30)",
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

	[Test]
	public void Show_SelectTheme_PersistsIdAndDisplaysNameAndVariant()
	{
		var harness = new Harness();

		harness.Answers.Answer(SettingsAction.SelectTheme);
		harness.Answers.Answer("EtcdBlue");
		harness.Answers.Cancel();

		harness.Screen.Show();

		Assert.Multiple(() =>
		{
			Assert.That(harness.Settings.Current.ThemeId, Is.EqualTo("EtcdBlue"));
			Assert.That(harness.Repository.Saved?.ThemeId, Is.EqualTo("EtcdBlue"));
			Assert.That(harness.Themes.Current.Id, Is.EqualTo("EtcdBlue"));
			Assert.That(harness.Answers.Prompt<string>(1).Items.Select(item => item.Label), Is.EqualTo(
			[
				"Reddy (Dark)",
				"Etcd Dark (Dark)",
				"Etcd Blue (Dark)",
				"Ubuntu (Dark)",
				"Manjaro (Dark)",
				"Pop! OS (Dark)",
				"Solarized (Dark)",
				"Dark Violet (Dark)",
				"Matrix (Dark)",
				"Light Reddy (Light)",
				"VS Code (Light)",
				"Solarized (Light)"
			]));
			Assert.That(harness.Answers.Prompt<SettingsAction>(2).Items[1].Label, Is.EqualTo("Theme (Etcd Blue (Black))"));
		});
	}

	[Test]
	public void Show_CancelledThemeMenu_RestoresThePreviewedTheme()
	{
		var harness = new Harness();

		harness.Answers.Answer(SettingsAction.SelectTheme);
		harness.Answers.Cancel();
		harness.Answers.Cancel();

		harness.Screen.Show();

		Assert.Multiple(() =>
		{
			Assert.That(harness.Themes.Current.Id, Is.EqualTo("Reddy"));
			Assert.That(harness.Repository.Saved, Is.Null);
		});
	}

	[Test]
	public void Show_SelectLanguage_PersistsCodeSwitchesTextAndAsksForRestart()
	{
		var harness = new Harness();

		harness.Answers.Answer(SettingsAction.SelectLanguage);
		harness.Answers.Answer("ru");

		var restart = harness.Screen.Show();

		Assert.Multiple(() =>
		{
			Assert.That(restart, Is.True);
			Assert.That(harness.Repository.Saved?.LanguageCode, Is.EqualTo("ru"));
			Assert.That(harness.Languages.Current.LanguageCode, Is.EqualTo("ru"));
			Assert.That(harness.Answers.Prompt<string>(1).SelectedId, Is.EqualTo("en"));
		});
	}

	[Test]
	public void FirstRun_WithoutSavedPreferences_AsksForLanguageThenTheme()
	{
		var harness = new Harness();

		harness.Answers.Answer("ru");
		harness.Answers.Answer("EtcdBlue");

		harness.Loader.Load();
		harness.Language.ShowIfMissing();
		harness.Theme.ShowIfMissing();

		Assert.Multiple(() =>
		{
			Assert.That(harness.Answers.Prompt<string>(0).Title, Is.EqualTo("Choose a language:"));
			Assert.That(harness.Answers.Prompt<string>(1).Title, Is.EqualTo("Выберите тему:"));
			Assert.That(harness.Repository.Saved, Is.EqualTo(new AppSettings { LanguageCode = "ru", ThemeId = "EtcdBlue" }));
			Assert.That(harness.Themes.Current.Id, Is.EqualTo("EtcdBlue"));
		});
	}

	[Test]
	public void FirstRun_WithSavedPreferences_AppliesThemWithoutAsking()
	{
		var harness = new Harness(new AppSettings { LanguageCode = "ru", ThemeId = "EtcdBlue" });

		harness.Loader.Load();
		harness.Language.ShowIfMissing();
		harness.Theme.ShowIfMissing();

		Assert.Multiple(() =>
		{
			Assert.That(harness.Answers.Prompts, Is.Empty);
			Assert.That(harness.Languages.Current.LanguageCode, Is.EqualTo("ru"));
			Assert.That(harness.Themes.Current.Id, Is.EqualTo("EtcdBlue"));
		});
	}

	[Test]
	public void FirstRun_WithUnknownSavedIds_FallsBackToTheDefaults()
	{
		var harness = new Harness(new AppSettings { LanguageCode = "xx", ThemeId = "Gone" });

		harness.Loader.Load();
		harness.Language.ShowIfMissing();
		harness.Theme.ShowIfMissing();

		Assert.Multiple(() =>
		{
			Assert.That(harness.Answers.Prompts, Is.Empty);
			Assert.That(harness.Languages.Current.LanguageCode, Is.EqualTo("en"));
			Assert.That(harness.Themes.Current.Id, Is.EqualTo("Reddy"));
		});
	}

	private sealed class Harness
	{
		public readonly FakeSelectionPrompt Answers = new();
		public readonly FakeScreenCanvas Canvas = new();
		public readonly RecordingSettingsRepository Repository;
		public readonly ThemeCatalog Themes = new();
		public readonly LocalizationCatalog Languages = new();
		public readonly AppSettingsStore Settings;
		public readonly PreferencesLoader Loader;
		public readonly LanguagePreferenceEditor Language;
		public readonly ThemePreferenceEditor Theme;
		public readonly SettingsScreen Screen;

		public Harness(AppSettings? stored = null)
		{
			Repository = new(stored);

			LocalizationCatalog localization = new();
			var statusBar = new StatusBar(new StubAppInfo(), new ConnectionSession(), localization);
			var prompt = new Prompt(new StubTextInput());
			var screen = new Screen(Canvas, new Header(), statusBar);
			var message = new Message(screen, new FakeKeyReader(), localization);
			var menu = new Menu(screen, Answers);

			Settings = new AppSettingsStore(Repository);
			Loader = new PreferencesLoader(Settings, Languages, Themes);

			Loader.Load();

			var writer = new SettingsWriter(Settings, message, localization);
			Language = new LanguagePreferenceEditor(menu, writer, Languages);
			Theme = new ThemePreferenceEditor(menu, writer, Themes, Languages);
			Screen = new SettingsScreen(menu, [Language, Theme, new PageSizeEditor(new UserInput(prompt, Settings), writer, message, localization), new TrimInputValuesEditor(writer, localization)], localization);
		}
	}

	private sealed class RecordingSettingsRepository(AppSettings? _stored) : IAppSettingsRepository
	{
		public AppSettings? Saved { get; private set; }

		public AppSettings Load() => Saved ?? _stored ?? new AppSettings();

		public void Save(AppSettings settings) => Saved = settings;
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
