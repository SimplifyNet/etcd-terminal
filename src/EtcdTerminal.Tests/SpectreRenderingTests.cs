using System.Text;
using EtcdTerminal.App.Theming;
using EtcdTerminal.Presentation;
using EtcdTerminal.Theming;
using EtcdTerminal.Infrastructure.Terminal;
using EtcdTerminal.Tests.Fakes;
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
	public void PanelRenderer_WritesLiteralTextWithoutTreatingBracketsAsMarkup()
	{
		var console = new TestConsole();

		Render(console, new PanelModel(
		[
			new PanelLine([new StyledText("service/[a:b]/ключ", TextRole.Accent)])
		]));

		Assert.That(console.Output, Does.Contain("service/[a:b]/ключ"));
	}

	[Test]
	public void PanelRenderer_WritesEveryLineOfTheModel()
	{
		var console = new TestConsole();

		Render(console, new PanelModel(
		[
			new PanelLine([new StyledText("first", TextRole.Muted)]),
			new PanelLine([new StyledText("second", TextRole.Primary)])
		]));

		Assert.That(console.Output, Does.Contain("first"));
		Assert.That(console.Output, Does.Contain("second"));
	}

	[Test]
	public void PanelRenderer_TablePanel_KeepsEveryRowOnOneLineAndWithinWidth()
	{
		var console = new TestConsole();

		Render(console, new PanelModel(
		[
			new PanelLine(
			[
				new StyledText(new string('k', 200), TextRole.Accent),
				new StyledText(new string('v', 200), TextRole.Accent)
			]),
			new PanelLine(
			[
				new StyledText("/a", TextRole.Primary),
				new StyledText("short", TextRole.Primary)
			])
		], PanelKind.Table));

		var lines = console.Output.Split('\n').Select(line => line.TrimEnd('\r')).ToList();
		var rows = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();

		Assert.That(rows, Has.Count.EqualTo(2), "one line per row");
		Assert.That(lines.All(line => line.Length <= Width), Is.True, "cells must be cropped to the panel width");
		Assert.That(console.Output, Does.Contain("/a"));
		Assert.That(console.Output, Does.Contain("short"));
	}

	[Test]
	public void PanelRenderer_TablePanel_KeepsColumnContentLiteral()
	{
		var console = new TestConsole();

		Render(console, new PanelModel(
		[
			new PanelLine(
			[
				new StyledText("service/[a:b]/\u043a\u043b\u044e\u0447", TextRole.Accent),
				new StyledText("value [x]", TextRole.Accent)
			])
		], PanelKind.Table));

		Assert.That(console.Output, Does.Contain("service/[a:b]/\u043a\u043b\u044e\u0447"));
		Assert.That(console.Output, Does.Contain("value [x]"));
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
	public void StatusBarRenderer_WithoutSession_WritesVersionOnly()
	{
		var console = new TestConsole();

		RenderStatusBar(console, new StatusBarModel { Version = new StyledText("0.4", TextRole.Primary) });

		Assert.That(console.Output, Does.Contain("v0.4"));
		Assert.That(console.Output, Does.Not.Contain("\u2022"));
	}

	[Test]
	public void StatusBarRenderer_KeepsDetailsOnTheRightEdgeWhenHintsChangeLength()
	{
		var renderer = StatusBarRenderer();

		var withShortHints = RenderLine(renderer, [new StyledText("\u2191/\u2193", TextRole.Primary)]);
		var withLongHints = RenderLine(renderer, [new StyledText("\u2191/\u2193 navigate confirm select Esc back", TextRole.Primary)]);

		Assert.That(ColumnOf(withLongHints, "v0.4"), Is.EqualTo(ColumnOf(withShortHints, "v0.4")),
			"session details must stay flush right regardless of how much room the hints take");
		Assert.That(ColumnOf(withShortHints, "v0.4"), Is.GreaterThan(40));
	}

	[Test]
	public void StatusBarRenderer_ShortensEndpointBeforeDroppingOtherFields()
	{
		var renderer = StatusBarRenderer();
		var model = new StatusBarModel
		{
			Name = new StyledText(new string('n', 60), TextRole.Secondary),
			Connection = new StyledText("http://" + new string('h', 90) + ":2379", TextRole.Muted),
			Username = new StyledText(new string('u', 40), TextRole.Warning),
			Version = new StyledText("0.4", TextRole.Primary)
		};

		var console = new TestConsole();

		console.Write(renderer.Build(Fit(model, 60)));

		var text = console.Output;

		Assert.That(text, Does.Contain("\u2026"));
		Assert.That(LineWidth(text), Is.LessThanOrEqualTo(80));
	}

	[Test]
	public void StatusBarRenderer_DropsUserThenEndpointThenHintsOnVeryNarrowWidth()
	{
		var renderer = StatusBarRenderer();
		var model = new StatusBarModel
		{
			Hints = [new StyledText("\u2191/\u2193", TextRole.Primary)],
			Name = new StyledText("prod", TextRole.Secondary),
			Connection = new StyledText("http://localhost:2379", TextRole.Muted),
			Username = new StyledText("root", TextRole.Warning),
			Version = new StyledText("0.4", TextRole.Primary)
		};

		var droppedUser = Fit(model, 45);

		Assert.That(droppedUser.Username, Is.Null);
		Assert.That(droppedUser.Connection?.Text, Does.EndWith("\u2026"));
		Assert.That(droppedUser.Hints, Is.Not.Empty);

		var droppedEndpoint = Fit(model, 30);

		Assert.That(droppedEndpoint.Username, Is.Null);
		Assert.That(droppedEndpoint.Connection, Is.Null);
		Assert.That(droppedEndpoint.Hints, Is.Not.Empty);

		var droppedHints = Fit(model, 15);

		Assert.That(droppedHints.Hints, Is.Empty);
		Assert.That(droppedHints.Name?.Text, Is.EqualTo("prod"));
	}

	[Test]
	public void StatusBarRenderer_ShortensConnectionNameOnlyAsLastResort()
	{
		var model = new StatusBarModel
		{
			Name = new StyledText(new string('n', 60), TextRole.Secondary),
			Connection = new StyledText("http://localhost:2379", TextRole.Muted),
			Version = new StyledText("0.4", TextRole.Primary)
		};

		var fitted = Fit(model, 40);

		Assert.That(fitted.Name?.Text, Is.EqualTo(new string('n', 23) + "\u2026"));
		Assert.That(fitted.Name?.Text, Has.Length.LessThanOrEqualTo(24));
	}

	[Test]
	public void StatusBarRenderer_KeepsModelUnchangedWhenItFits()
	{
		var renderer = StatusBarRenderer();
		var model = new StatusBarModel
		{
			Name = new StyledText("prod", TextRole.Secondary),
			Connection = new StyledText("http://localhost:2379", TextRole.Muted),
			Version = new StyledText("0.4", TextRole.Primary)
		};

		Assert.That(Fit(model, 200), Is.SameAs(model));
	}

	private const int Width = 80;

	[Test]
	public void StatusBarRenderer_ShortTerminal_LeavesTheScreenAndCursorAlone()
	{
		var console = new TestConsole();
		var cursor = new RecordingTerminal();

		console.Profile.Height = 2;

		StatusBarRenderer(console, cursor).EnsureRoomAbove(1);

		Assert.That(console.Output, Is.Empty);
		Assert.That(cursor.CursorSets, Is.Empty);
	}

	[Test]
	public void StatusBarRenderer_CursorClearOfTheFooter_DoesNotScroll()
	{
		var console = new TestConsole();
		var cursor = new RecordingTerminal { CursorTop = 5 };

		console.Profile.Height = 24;

		StatusBarRenderer(console, cursor).EnsureRoomAbove(1);

		Assert.That(console.Output, Is.Empty);
		Assert.That(cursor.CursorSets, Is.Empty);
	}

	[Test]
	public void StatusBarRenderer_CursorInsideTheFooterReserve_ScrollsTheScreen()
	{
		var console = new TestConsole();
		var cursor = new RecordingTerminal { CursorTop = 23 };

		console.Profile.Height = 24;

		StatusBarRenderer(console, cursor).EnsureRoomAbove(1);

		Assert.That(console.Output, Is.Not.Empty);
	}

	private static SpectreStatusBarRenderer StatusBarRenderer(TestConsole console, RecordingTerminal cursor) =>
		new(console, new RoleStyleMapper(new ReddyTheme()), cursor);

	private static SpectrePanelRenderer PanelRenderer() => new(new TestConsole(), new RoleStyleMapper(new ReddyTheme()), new ReddyTheme());

	private static SpectreStatusBarRenderer StatusBarRenderer() => new(new TestConsole(), new RoleStyleMapper(new ReddyTheme()), new RecordingTerminal());

	private static StatusBarModel Fit(StatusBarModel model, int width) => SpectreStatusBarRenderer.Fit(model, width);

	private static void Render(TestConsole console, PanelModel panel) =>
		console.Write(PanelRenderer().Build(panel, Width));

	private static void RenderStatusBar(TestConsole console, StatusBarModel model) =>
		console.Write(StatusBarRenderer().Build(model));

	private static string RenderLine(SpectreStatusBarRenderer renderer, IReadOnlyList<StyledText> hints)
	{
		var console = new TestConsole();

		console.Write(renderer.Build(new StatusBarModel
		{
			Hints = hints,
			Name = new StyledText("prod", TextRole.Secondary),
			Version = new StyledText("0.4", TextRole.Primary)
		}));

		return console.Lines.Single(text => text.Contains("prod")).TrimEnd();
	}

	private static int ColumnOf(string line, string needle) => line.IndexOf(needle, StringComparison.Ordinal);

	private static int LineWidth(string output) =>
		output.Split('\n').Max(line => line.TrimEnd('\r').Length);

	private static bool Mapper(RoleStyleMapper mapper, TextRole role, RgbColor expected) =>
		mapper.Resolve(role) == new Style(foreground: new Color(expected.R, expected.G, expected.B));
}
