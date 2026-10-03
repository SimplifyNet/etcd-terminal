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

	private static SpectreSelectionPrompt Prompt(TestConsole console) =>
		new(console, new RoleStyleMapper(new ReddyTheme()), new EnglishLocalization());
}
