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
public sealed class ScreenFrameTests
{
	[Test]
	public void Show_OnCancel_ReturnsNullAndKeepsTheScreenItOpened()
	{
		var harness = new Harness();

		harness.Answers.Cancel();

		var chosen = harness.Menu.Show("title", [new Choice<int>(1, "prod")]);

		Assert.Multiple(() =>
		{
			Assert.That(chosen, Is.Null);
			Assert.That(harness.Canvas.NewScreenCount, Is.EqualTo(1), "the screen is opened once for the prompt");
		});
	}

	[Test]
	public void Show_WritesThePreambleBeforeThePrompt()
	{
		var harness = new Harness();

		var preamble = TextBlock.Line(new StyledText("notice", TextRole.Warning));

		harness.Answers.Cancel();

		harness.Menu.Show("title", [new Choice<int>(1, "prod")], [preamble]);

		Assert.Multiple(() =>
		{
			Assert.That(harness.Canvas.NewScreenCount, Is.EqualTo(1));
			Assert.That(harness.Canvas.Blocks, Has.Count.EqualTo(2), "banner and preamble are the only writes");
			Assert.That(harness.Canvas.Blocks[0], Is.InstanceOf<BannerBlock>());
			Assert.That(harness.Canvas.Blocks[1], Is.SameAs(preamble), "the preamble lands under the banner before the prompt runs");
		});
	}

	private sealed class Harness
	{
		public readonly FakeSelectionPrompt Answers = new();
		public readonly FakeScreenCanvas Canvas = new();
		public readonly Menu Menu;

		public Harness()
		{
			var statusBar = new StatusBar(new StubAppInfo(), new ConnectionSession(), new EnglishLocalization());

			Menu = new Menu(new Screen(Canvas, new Header(), statusBar), Answers);
		}
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
