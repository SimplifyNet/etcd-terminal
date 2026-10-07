using EtcdTerminal.App.Theming;
using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Theming;
using EtcdTerminal.Infrastructure.Terminal;
using NUnit.Framework;
using Spectre.Console;
using Spectre.Console.Testing;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class SpectreRenderingTests
{
	[Test]
	public void RoleStyleMapper_MapsEveryRoleToTheThemePalette()
	{
		var theme = new ReddyTheme();
		var mapper = new RoleStyleMapper(theme);

		Assert.That(Mapper(mapper, TextRole.Primary, theme.Primary), Is.True);
		Assert.That(Mapper(mapper, TextRole.Secondary, theme.Secondary), Is.True);
		Assert.That(Mapper(mapper, TextRole.Success, theme.Success), Is.True);
		Assert.That(Mapper(mapper, TextRole.Danger, theme.Danger), Is.True);
		Assert.That(Mapper(mapper, TextRole.Warning, theme.Warning), Is.True);
		Assert.That(Mapper(mapper, TextRole.Muted, theme.Muted), Is.True);
		Assert.That(Mapper(mapper, TextRole.Subtle, theme.Subtle), Is.True);
		Assert.That(Mapper(mapper, TextRole.Accent, theme.Accent), Is.True);
	}

	[Test]
	public void BlockRenderer_WritesLiteralTextWithoutTreatingBracketsAsMarkup()
	{
		var console = new TestConsole();

		Render(console, TextBlock.Line(new StyledText("service/[a:b]/ключ", TextRole.Accent)));

		Assert.That(console.Output, Does.Contain("service/[a:b]/ключ"));
	}

	[Test]
	public void BlockRenderer_WritesEveryLineOfTheModel()
	{
		var console = new TestConsole();

		Render(console, new TextBlock(
		[
			[new StyledText("first", TextRole.Muted)],
			[new StyledText("second", TextRole.Primary)]
		]));

		Assert.That(console.Output, Does.Contain("first"));
		Assert.That(console.Output, Does.Contain("second"));
	}

	[Test]
	public void BlockRenderer_StartsEveryLineOnTheFourthColumn()
	{
		var console = new TestConsole();

		Render(console, TextBlock.Line(new StyledText("value", TextRole.Primary)));

		Assert.That(console.Lines[0], Does.StartWith(ContentIndent.Text));
	}

	[Test]
	public void TextInput_MarginsThePromptOnTheFourthColumn()
	{
		var console = new TestConsole().Interactive();

		console.Input.PushKey(ConsoleKey.Enter);

		new SpectreTextInput(new EscapableConsole(console)).ReadLine("Value:");

		Assert.That(console.Output, Does.Contain(ContentIndent.Text + "Value:"));
	}

	[Test]
	public void BlockRenderer_TableBlock_KeepsColumnContentLiteral()
	{
		var console = new TestConsole();

		Render(console, new TableBlock(
		[],
		[
			[
				new StyledText("service/[a:b]/\u043a\u043b\u044e\u0447", TextRole.Accent),
				new StyledText("value [x]", TextRole.Accent)
			]
		]));

		Assert.That(console.Output, Does.Contain("service/[a:b]/\u043a\u043b\u044e\u0447"));
		Assert.That(console.Output, Does.Contain("value [x]"));
	}

	[Test]
	public void BlockRenderer_TableBlock_ShowsTheHeaderRowLiterally()
	{
		var console = new TestConsole();

		Render(console, new TableBlock(
		[new StyledText("scope [x]", TextRole.Muted)],
		[[new StyledText("/a", TextRole.Primary), new StyledText("value", TextRole.Primary)]]));

		Assert.That(console.Output, Does.Contain("scope [x]"));
		Assert.That(console.Output, Does.Contain("/a"));
	}

	[Test]
	public void BlockRenderer_TableBlock_FramesOnlyTablesMarkedFramed()
	{
		var framed = new TestConsole();

		Render(framed, new TableBlock(
		[new StyledText("scope", TextRole.Muted)],
		[[new StyledText("/a", TextRole.Primary)]]) { IsFramed = true });

		var plain = new TestConsole();

		Render(plain, new TableBlock(
		[new StyledText("scope", TextRole.Muted)],
		[[new StyledText("/a", TextRole.Primary)]]));

		Assert.That(framed.Output, Does.Contain("┌"));
		Assert.That(plain.Output, Does.Not.Contain("┌"));
	}

	[Test]
	public void BlockRenderer_TableBlock_SplitsTheRegionInHalfForBothColumns()
	{
		var console = new TestConsole();

		Render(console, new TableBlock(
		[],
		[[new StyledText("/alpha", TextRole.Primary), new StyledText("one", TextRole.Primary)]]));

		var row = console.Lines.Single(line => line.Contains("one"));

		Assert.That(row.IndexOf("one", StringComparison.Ordinal), Is.EqualTo(42), row);
	}

	[Test]
	public void StatusBarRenderer_WritesSessionDetailsAndVersion()
	{
		var console = new TestConsole();

		RenderStatusBar(console, new StatusBarModel
		{
			Hints = [new StyledText("\u2191/\u2193", TextRole.Primary)],
			Name = new StyledText("prod", TextRole.Secondary),
			Connection = new StyledText("http://localhost:2379", TextRole.Muted),
			Username = new StyledText("root", TextRole.Warning),
			Version = new StyledText("0.4", TextRole.Primary)
		});

		Assert.That(console.Output, Does.Contain("prod"));
		Assert.That(console.Output, Does.Contain("http://localhost:2379"));
		Assert.That(console.Output, Does.Contain("root"));
		Assert.That(console.Output, Does.Contain("v0.4"));
	}

	[Test]
	public void StatusBarRenderer_PaintsThreeRowsInTheBandBackground()
	{
		var console = new TestConsole { EmitAnsiSequences = true };

		RenderStatusBar(console, new StatusBarModel { Version = new StyledText("0.4", TextRole.Primary) });

		Assert.Multiple(() =>
		{
			Assert.That(console.Output, Does.Contain("48;2;27;28;30"), console.Output);
			Assert.That(console.Lines.Count, Is.EqualTo(3), console.Output);
		});
	}

	[Test]
	public void BlockRenderer_Band_PaintsThreeRowsInTheBandBackground()
	{
		var console = new TestConsole { EmitAnsiSequences = true };

		Render(console, TextBlock.Line(new StyledText("keep me", TextRole.Primary)) with { Band = true });

		Assert.Multiple(() =>
		{
			Assert.That(console.Lines.Count, Is.EqualTo(3), console.Output);
			Assert.That(console.Output, Does.Contain("48;2;27;28;30"), console.Output);
			Assert.That(console.Output, Does.Contain("keep me"));
		});
	}

	[Test]
	public void BlockRenderer_Band_KeepsTheSecondColumnOnTheContentRow()
	{
		var console = new TestConsole();

		Render(console, TextBlock.Line(new StyledText("Type to search", TextRole.Muted)) with { Band = true });

		Assert.Multiple(() =>
		{
			Assert.That(console.Lines, Has.Count.EqualTo(3), console.Output);
			Assert.That(console.Lines[0].Trim(), Is.Empty);
			Assert.That(console.Lines[1], Does.StartWith("  Type to search"));
			Assert.That(console.Lines[2].Trim(), Is.Empty);
		});
	}

	[Test]
	public void StatusBarRenderer_WithoutSession_WritesVersionOnly()
	{
		var console = new TestConsole();

		RenderStatusBar(console, new StatusBarModel { Version = new StyledText("0.4", TextRole.Primary) });

		Assert.That(console.Output, Does.Contain("v0.4"));
		Assert.That(console.Output, Does.Not.Contain("\u2022"));
	}

	[Test]
	public void StatusBarRenderer_KeepsBracketsInTheConnectionLiteral()
	{
		var console = new TestConsole();

		RenderStatusBar(console, new StatusBarModel
		{
			Name = new StyledText("prod", TextRole.Secondary),
			Connection = new StyledText("http://[::1]:2379", TextRole.Muted),
			Version = new StyledText("0.4", TextRole.Primary)
		});

		Assert.That(console.Output, Does.Contain("http://[::1]:2379"));
	}

	[Test]
	public void StatusBarRenderer_WritesTheVersionNextToTheHints()
	{
		var console = new TestConsole();

		RenderStatusBar(console, new StatusBarModel
		{
			Hints = [new StyledText("\u2191/\u2193", TextRole.Primary)],
			Version = new StyledText("0.4", TextRole.Primary)
		});

		Assert.That(console.Output, Does.Contain("\u2191/\u2193"));
		Assert.That(console.Output, Does.Contain("v0.4"));
	}

	[Test]
	public void StatusBarRenderer_DropsSessionFieldsInsteadOfWrapping()
	{
		var console = new TestConsole();

		RenderStatusBar(console, new StatusBarModel
		{
			Hints = [new StyledText("\u2191/\u2193 navigate \u00b7 Enter confirm/select \u00b7 Esc back", TextRole.Primary)],
			Name = new StyledText("etcd-local", TextRole.Secondary),
			Connection = new StyledText("http://localhost:2379", TextRole.Muted),
			Username = new StyledText("root", TextRole.Warning),
			Version = new StyledText("0.9", TextRole.Primary)
		});

		Assert.Multiple(() =>
		{
			Assert.That(console.Lines.Count, Is.EqualTo(3), console.Output);
			Assert.That(console.Output, Does.Contain("Esc back"));
			Assert.That(console.Output, Does.Contain("etcd-local"));
			Assert.That(console.Output, Does.Contain("v0.9"));
			Assert.That(console.Output, Does.Not.Contain("localhost:2379"));
		});
	}

	private static BlockRenderer BlockRenderer() => new(new RoleStyleMapper(new ReddyTheme()), new ReddyTheme());

	private static StatusBarRenderer StatusBarRenderer(TestConsole console) => new(console, new RoleStyleMapper(new ReddyTheme()), new ReddyTheme());

	private static void Render(TestConsole console, Block block) =>
		console.Write(BlockRenderer().Render(block));

	private static void RenderStatusBar(TestConsole console, StatusBarModel model) =>
		console.Write(StatusBarRenderer(console).Build(model));

	private static bool Mapper(RoleStyleMapper mapper, TextRole role, RgbColor expected) =>
		mapper.Resolve(role) == new Style(foreground: new Color(expected.R, expected.G, expected.B));
}
