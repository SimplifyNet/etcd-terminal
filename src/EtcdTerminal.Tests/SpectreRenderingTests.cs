using EtcdTerminal.App.Theming;
using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Theming;
using EtcdTerminal.Infrastructure.Terminal;
using NUnit.Framework;
using Spectre.Console;
using Spectre.Console.Testing;
using Spectre.Console.Rendering;
using EtcdTerminal.App.Theming.Themes;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class SpectreRenderingTests
{
	[Test]
	public void RoleStyleMapper_MapsEveryRoleToTheThemePalette()
	{
		var theme = new ReddyTheme();
		var themes = Themes();
		var mapper = new RoleStyleMapper(themes);

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
	public void LightReddyTheme_HasReadableTextContrastForStandardRolesOnEveryLightBackground()
	{
		var theme = new LightReddyTheme();
		RgbColor[] backgrounds = [theme.WindowBackground, theme.BandBackground, theme.ActionPanelTitleBackground];
		RgbColor[] foregrounds = [theme.Primary, theme.Secondary, theme.Success, theme.Danger, theme.Warning, theme.Muted, theme.Subtle];

		foreach (var foreground in foregrounds)
			foreach (var background in backgrounds)
				Assert.That(Contrast(foreground, background), Is.GreaterThanOrEqualTo(4.5), $"{foreground} on {background}");
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
		[[new StyledText("/a", TextRole.Primary)]])
		{ IsFramed = true });

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
	public void BlockRenderer_TableBlock_SplitsTheRegionInThirdsForThreeColumns()
	{
		var console = new TestConsole();

		Render(console, new TableBlock(
		[],
		[[new StyledText("user01", TextRole.Primary), new StyledText("role01", TextRole.Primary), new StyledText("perm01", TextRole.Primary)]]));

		var row = console.Lines.Single(line => line.Contains("perm01"));

		Assert.That(row.IndexOf("user01", StringComparison.Ordinal), Is.EqualTo(4), row);
		Assert.That(row.IndexOf("role01", StringComparison.Ordinal), Is.EqualTo(29), row);
		Assert.That(row.IndexOf("perm01", StringComparison.Ordinal), Is.EqualTo(54), row);
	}

	[Test]
	public void BlockRenderer_HeaderedTable_KeepsTwoColumnsFiftyWide()
	{
		var console = new TestConsole().Width(120);

		Render(console, new TableBlock(
		[new StyledText("Username", TextRole.Accent), new StyledText("Roles", TextRole.Accent)],
		[[new StyledText("alice", TextRole.Primary), new StyledText("dev", TextRole.Primary)]]));

		var row = console.Lines.Single(line => line.Contains("dev"));

		Assert.That(row.IndexOf("alice", StringComparison.Ordinal), Is.EqualTo(4), row);
		Assert.That(row.IndexOf("dev", StringComparison.Ordinal), Is.EqualTo(54), row);
	}

	[Test]
	public void BlockRenderer_HeaderedTable_ShrinksThreeColumnsEquallyWhenFiftyDoesNotFit()
	{
		var console = new TestConsole().Width(120);

		Render(console, new TableBlock(
		[new StyledText("User", TextRole.Accent), new StyledText("Role", TextRole.Accent), new StyledText("Permission", TextRole.Accent)],
		[[new StyledText("alice", TextRole.Primary), new StyledText("dev", TextRole.Primary), new StyledText("Read", TextRole.Primary)]]));

		var row = console.Lines.Single(line => line.Contains("Read"));

		Assert.That(row.IndexOf("alice", StringComparison.Ordinal), Is.EqualTo(4), row);
		Assert.That(row.IndexOf("dev", StringComparison.Ordinal), Is.EqualTo(42), row);
		Assert.That(row.IndexOf("Read", StringComparison.Ordinal), Is.EqualTo(80), row);
	}

	[Test]
	public void BlockRenderer_HeaderedTable_NeverWidensAColumnPastFifty()
	{
		var console = new TestConsole().Width(200);

		Render(console, new TableBlock(
		[new StyledText("Username", TextRole.Accent), new StyledText("Roles", TextRole.Accent)],
		[[new StyledText("alice", TextRole.Primary), new StyledText(new string('x', 80), TextRole.Primary)]]));

		var row = console.Lines.Single(line => line.Contains("alice"));

		Assert.That(row.IndexOf('x'), Is.EqualTo(54), row);
		Assert.That(row.Count(character => character == 'x'), Is.LessThanOrEqualTo(50), row);
	}

	[Test]
	public void BlockRenderer_HeaderedTable_ShrinksEveryColumnEquallyOnANarrowRegion()
	{
		var console = new TestConsole();

		Render(console, new TableBlock(
		[new StyledText("User", TextRole.Accent), new StyledText("Role", TextRole.Accent), new StyledText("Permission", TextRole.Accent)],
		[[new StyledText("user01", TextRole.Primary), new StyledText("role01", TextRole.Primary), new StyledText("perm01", TextRole.Primary)]]));

		var row = console.Lines.Single(line => line.Contains("perm01"));

		Assert.That(row.IndexOf("role01", StringComparison.Ordinal), Is.EqualTo(29), row);
		Assert.That(row.IndexOf("perm01", StringComparison.Ordinal), Is.EqualTo(54), row);
	}

	[Test]
	public void BlockRenderer_HeaderedTable_LeavesABlankLineUnderTheHeader()
	{
		var console = new TestConsole();

		Render(console, new TableBlock(
		[new StyledText("Username", TextRole.Accent), new StyledText("Roles", TextRole.Accent)],
		[[new StyledText("alice", TextRole.Primary), new StyledText("dev", TextRole.Primary)]]));

		var header = console.Lines.ToList().FindIndex(line => line.Contains("Username"));
		var data = console.Lines.ToList().FindIndex(line => line.Contains("alice"));

		Assert.That(data - header, Is.EqualTo(2), console.Output);
		Assert.That(console.Lines[header + 1].Trim(), Is.Empty, console.Output);
	}

	[Test]
	public void BlockRenderer_HeaderedTable_DrawsTheHeaderInItsRoleColor()
	{
		var console = new TestConsole { EmitAnsiSequences = true };
		var accent = new ReddyTheme().Accent;

		Render(console, new TableBlock(
		[new StyledText("Username", TextRole.Accent)],
		[[new StyledText("alice", TextRole.Primary)]]));

		Assert.That(console.Output, Does.Contain($"38;2;{accent.R};{accent.G};{accent.B}m"), console.Output);
	}

	[Test]
	public void BlockRenderer_HeaderlessTable_HasNoBlankRowAndSplitsEqually()
	{
		var console = new TestConsole().Width(120);

		Render(console, new TableBlock(
		[],
		[[new StyledText("a", TextRole.Primary), new StyledText("b", TextRole.Primary)]]));

		var row = console.Lines.Single(line => line.Contains('b'));

		Assert.That(console.Lines.Count(line => line.Trim().Length > 0), Is.EqualTo(1), console.Output);
		Assert.That(row.IndexOf('b'), Is.EqualTo(62), row);
	}

	[Test]
	public void BlockRenderer_PointerTable_HangsThePointerOnTheMarginColumn()
	{
		var console = new TestConsole();

		Render(console, new TableBlock(
		[],
		[
			[new StyledText("\u276f ", TextRole.Accent), new StyledText("/alpha", TextRole.Accent), new StyledText("one", TextRole.Accent)],
			[new StyledText("  ", TextRole.Primary), new StyledText("/beta", TextRole.Primary), new StyledText("two", TextRole.Primary)]
		])
		{ Pointer = true });

		var selected = console.Lines.First(line => line.Contains("/alpha"));
		var plain = console.Lines.First(line => line.Contains("/beta"));

		Assert.That(selected, Does.StartWith("  \u276f /alpha"), selected);
		Assert.That(selected.IndexOf("one", StringComparison.Ordinal), Is.EqualTo(42), selected);
		Assert.That(plain.IndexOf("/beta", StringComparison.Ordinal), Is.EqualTo(4), plain);
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
	public void BlockRenderer_ActionPanel_UsesBandBackgroundAndAccentStripe()
	{
		var console = new TestConsole { EmitAnsiSequences = true };
		var plainConsole = new TestConsole();
		var theme = new ReddyTheme();
		var selectedBackground = new Color(theme.ActionPanelTitleBackground.R, theme.ActionPanelTitleBackground.G, theme.ActionPanelTitleBackground.B);
		var panel = new ActionPanelBlock(
			[new StyledText("Selected: ", TextRole.Muted), new StyledText("/a", TextRole.Accent)],
			[new StyledText("E Edit", TextRole.Primary)]);

		Render(console, panel);
		Render(plainConsole, panel);
		var panelLines = Segment.SplitLines(BlockRenderer().Render(panel).Render(RenderOptions.Create(plainConsole), plainConsole.Profile.Width));

		Assert.Multiple(() =>
		{
			Assert.That(console.Output, Does.Contain($"48;2;{theme.BandBackground.R};{theme.BandBackground.G};{theme.BandBackground.B}"), console.Output);
			Assert.That(console.Output, Does.Contain($"48;2;{theme.ActionPanelTitleBackground.R};{theme.ActionPanelTitleBackground.G};{theme.ActionPanelTitleBackground.B}"), console.Output);
			Assert.That(console.Output, Does.Contain($"38;2;{theme.Accent.R};{theme.Accent.G};{theme.Accent.B}"), console.Output);
			Assert.That(console.Output, Does.Contain("\u2503"), console.Output);
			Assert.That(console.Output, Does.Contain("Selected:"), console.Output);
			Assert.That(console.Output, Does.Contain("/a"), console.Output);
			var titleLine = plainConsole.Lines.Single(line => line.Contains("Selected:", StringComparison.Ordinal));
			var stripeColumn = titleLine.IndexOf('\u2503');

			Assert.That(plainConsole.Lines.Any(line => line.Contains("E Edit", StringComparison.Ordinal)), Is.True, plainConsole.Output);
			Assert.That(plainConsole.Lines, Has.Count.EqualTo(6), plainConsole.Output);
			Assert.That(stripeColumn, Is.EqualTo(0), plainConsole.Output);
			Assert.That(titleLine.IndexOf("Selected:", StringComparison.Ordinal) - stripeColumn, Is.EqualTo(3), plainConsole.Output);
			Assert.That(panelLines[0].Concat(panelLines[1]).Concat(panelLines[2]).All(segment => segment.Style.Background == selectedBackground), Is.True, plainConsole.Output);
			Assert.That(panelLines[3].Concat(panelLines[4]).Concat(panelLines[5]).All(segment => segment.Style.Background == new Style(background: new Color(theme.BandBackground.R, theme.BandBackground.G, theme.BandBackground.B)).Background), Is.True, plainConsole.Output);
			Assert.That(panelLines[3].All(segment => segment.Style.Background == new Style(background: new Color(theme.BandBackground.R, theme.BandBackground.G, theme.BandBackground.B)).Background), Is.True, plainConsole.Output);
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

	private static BlockRenderer BlockRenderer()
	{
		var themes = Themes();

		return new(new RoleStyleMapper(themes), themes);
	}

	private static StatusBarRenderer StatusBarRenderer(TestConsole console)
	{
		var themes = Themes();

		return new(console, new RoleStyleMapper(themes), themes);
	}

	private static ThemeCatalog Themes() => new();

	private static void Render(TestConsole console, Block block) =>
		console.Write(BlockRenderer().Render(block));

	private static void RenderStatusBar(TestConsole console, StatusBarModel model) =>
		console.Write(StatusBarRenderer(console).Build(model));

	private static bool Mapper(RoleStyleMapper mapper, TextRole role, RgbColor expected) =>
		mapper.Resolve(role) == new Style(foreground: new Color(expected.R, expected.G, expected.B));

	private static double Contrast(RgbColor foreground, RgbColor background)
	{
		var foregroundLuminance = Luminance(foreground);
		var backgroundLuminance = Luminance(background);

		return (Math.Max(foregroundLuminance, backgroundLuminance) + 0.05) / (Math.Min(foregroundLuminance, backgroundLuminance) + 0.05);
	}

	private static double Luminance(RgbColor color)
	{
		var red = Linear(color.R / 255d);
		var green = Linear(color.G / 255d);
		var blue = Linear(color.B / 255d);

		return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
	}

	private static double Linear(double channel) =>
		channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
}
