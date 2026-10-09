using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Localization;
using EtcdTerminal.Environment;
using EtcdTerminal.Presentation;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class MenuTests
{
	[Test]
	public void Show_WithDuplicateLabels_ReturnsTheAnsweredChoice()
	{
		var harness = new Harness();

		IReadOnlyList<Choice<int>> items = [new(1, "same"), new(2, "same")];

		harness.Answers.Answer(2);

		var chosen = harness.Menu.Show("title", items);

		Assert.That(chosen?.Id, Is.EqualTo(2));
	}

	[Test]
	public void Show_OffersEveryLabelUnchanged()
	{
		var harness = new Harness();

		IReadOnlyList<Choice<int>> items = [new(1, "http://[::1]:2379")];

		harness.Answers.Cancel();

		harness.Menu.Show("title", items);

		var offered = harness.Answers.Prompt<int>(0);

		Assert.Multiple(() =>
		{
			Assert.That(offered.Title, Is.EqualTo("title"));
			Assert.That(offered.Items.Single().Label, Is.EqualTo("http://[::1]:2379"));
		});
	}

	[Test]
	public void Show_WithoutItems_ReturnsNullAndLeavesTheScreenClosed()
	{
		var harness = new Harness();

		var chosen = harness.Menu.Show<int>("title", []);

		Assert.Multiple(() =>
		{
			Assert.That(chosen, Is.Null);
			Assert.That(harness.Canvas.NewScreenCount, Is.Zero);
			Assert.That(harness.Answers.Prompts, Is.Empty);
		});
	}

	private sealed class Harness
	{
		public readonly FakeSelectionPrompt Answers = new();
		public readonly FakeScreenCanvas Canvas = new();
		public readonly Menu Menu;

		public Harness()
		{
			var statusBar = new StatusBar(new StubAppInfo(), new ConnectionSession(), new LocalizationCatalog());

			Menu = new Menu(new Screen(Canvas, new Header(), statusBar), Answers);
		}
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
