using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Theming;
using EtcdTerminal.Infrastructure.Terminal;
using EtcdTerminal.Presentation;
using NUnit.Framework;
using Spectre.Console.Testing;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class SpectreSelectionPromptTests
{
	[Test]
	public void Select_ReturnsTheChoiceTheCursorMovedTo()
	{
		var console = new TestConsole().Interactive();

		console.Input.PushKey(ConsoleKey.DownArrow);
		console.Input.PushKey(ConsoleKey.Enter);

		var selected = Prompt(console).Select(new ChoiceList<int>(null, [new(1, "first"), new(2, "second")]));

		Assert.That(selected?.Id, Is.EqualTo(2));
	}

	[Test]
	public void Select_NotifiesWhenTheHighlightedChoiceChanges()
	{
		var console = new TestConsole().Interactive();
		var highlighted = new List<int>();

		console.Input.PushKey(ConsoleKey.DownArrow);
		console.Input.PushKey(ConsoleKey.Enter);

		Prompt(console).Select(new ChoiceList<int>(null, [new(1, "first"), new(2, "second")]), choice => highlighted.Add(choice.Id));

		Assert.That(highlighted, Is.EqualTo([1, 2]));
	}

	[Test]
	public void Select_OnEscape_ReturnsNull()
	{
		var console = new TestConsole().Interactive();

		console.Input.PushKey(ConsoleKey.Escape);

		var selected = Prompt(console).Select(new ChoiceList<int>(null, [new(1, "first")]));

		Assert.That(selected, Is.Null);
	}

	[Test]
	public void Select_ShowsTheLabelLiterally()
	{
		var console = new TestConsole().Interactive();

		console.Input.PushKey(ConsoleKey.Enter);

		var selected = Prompt(console).Select(new ChoiceList<int>(null, [new(1, "[x] delete")]));

		Assert.Multiple(() =>
		{
			Assert.That(console.Output, Does.Contain("[x] delete"));
			Assert.That(selected?.Id, Is.EqualTo(1));
		});
	}

	[Test]
	public void Select_PointsAtTheCurrentRowAndMarginsEveryRow()
	{
		var console = new TestConsole().Interactive();

		console.Input.PushKey(ConsoleKey.Enter);

		Prompt(console).Select(new ChoiceList<int>("pick", [new(1, "first"), new(2, "second")]));

		Assert.Multiple(() =>
		{
			Assert.That(console.Output, Does.Contain("  ❯ first"));
			Assert.That(console.Output, Does.Contain("    second"));
			Assert.That(console.Output, Does.Contain("    pick"));
		});
	}

	[Test]
	public void Select_UsesTheThemePrimaryColorForUnselectedItems()
	{
		var console = new TestConsole { EmitAnsiSequences = true }.Interactive();
		var themes = new ThemeCatalog();
		themes.Set("LightReddy");

		console.Input.PushKey(ConsoleKey.Enter);

		new SpectreSelectionPrompt(console, new RoleStyleMapper(themes), new LocalizationCatalog())
			.Select(new ChoiceList<int>(null, [new(1, "selected"), new(2, "unselected")]));

		var primary = themes.Current.Primary;

		Assert.That(console.Output, Does.Contain($"38;2;{primary.R};{primary.G};{primary.B}m"), console.Output);
	}

	[Test]
	public void Select_ScrollHintAppearsWhenTheWindowIsSmallerThanTheList()
	{
		var console = new TestConsole().Interactive();

		console.Input.PushKey(ConsoleKey.Enter);

		var items = Enumerable.Range(0, 40).Select(id => new Choice<int>(id, $"item {id}")).ToArray();

		Prompt(console).Select(new ChoiceList<int>(null, items));

		Assert.That(console.Output, Does.Contain(ContentIndent.Text + new EnglishLocalization().MoreChoices));
	}

	private static SpectreSelectionPrompt Prompt(TestConsole console) =>
		new(console, new RoleStyleMapper(new ThemeCatalog()), new LocalizationCatalog());
}
