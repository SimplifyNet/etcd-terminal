using EtcdTerminal.App.Components;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens.Keys;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation;
using EtcdTerminal.Security;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class PanelModelTests
{
	[Test]
	public void Pagination_EmitsPageAndTotalsAsSeparateRoles()
	{
		var model = Layout().Pagination(0, 5, 42);
		var spans = model.Lines.Single().Spans;

		Assert.That(model.Kind, Is.EqualTo(PanelKind.Default));
		Assert.That(spans.Select(span => span.Text), Is.EqualTo(new[] { "Page ", "1/5", "  \u2022  ", "42", " total keys" }));
		Assert.That(spans.Select(span => span.Role), Is.EqualTo(new[]
		{
			TextRole.Muted,
			TextRole.Primary,
			TextRole.Muted,
			TextRole.Primary,
			TextRole.Muted
		}));
	}

	[Test]
	public void Actions_WithModify_OffersEditDeleteAndCancel()
	{
		var layout = Layout();

		Assert.That(layout.Selected("mykey").Kind, Is.EqualTo(PanelKind.Selection));

		var actions = layout.Actions(canModify: true);

		Assert.That(actions.Kind, Is.EqualTo(PanelKind.Actions));
		Assert.That(actions.Lines.Single().Text, Does.Contain("E Edit"));
		Assert.That(actions.Lines.Single().Text, Does.Contain("D Delete"));
		Assert.That(actions.Lines.Single().Text, Does.Contain("Esc Cancel"));
	}

	[Test]
	public void Actions_WithoutModify_OffersOnlyCancel()
	{
		var hints = Layout().Actions(canModify: false).Lines.Single().Text;

		Assert.That(hints, Does.Contain("Esc Cancel"));
		Assert.That(hints, Does.Not.Contain("Edit"));
		Assert.That(hints, Does.Not.Contain("Delete"));
	}

	[Test]
	public void Selected_KeepsSelectedKeyLiteral()
	{
		var selection = Layout().Selected("service/[a:b]/\u043a\u043b\u044e\u0447");

		Assert.That(selection.Lines.Single().Text, Is.EqualTo("Selected: service/[a:b]/\u043a\u043b\u044e\u0447"));
		Assert.That(selection.Lines.Single().Spans[1].Role, Is.EqualTo(TextRole.Accent));
	}

	[Test]
	public void Selected_NormalizesControlCharactersInSelectedKey()
	{
		var model = Layout().Selected("a\nb");

		Assert.That(model.Lines.Single().Text, Is.EqualTo("Selected: a\u23CEb"));
	}

	[Test]
	public void KeyList_MarksTheSelectedRowAndKeepsThreeColumns()
	{
		var keys = new[]
		{
			new EtcdKeyValue { Key = "/a", Value = "one" },
			new EtcdKeyValue { Key = "/b", Value = "two" }
		};

		var model = Layout().KeyList(keys, selectedIndex: 1);

		Assert.That(model.Kind, Is.EqualTo(PanelKind.Table));
		Assert.That(model.Lines, Has.Count.EqualTo(2));
		Assert.That(model.Lines[0].Spans[0].Text, Does.StartWith("    "));
		Assert.That(model.Lines[1].Spans[0].Text, Does.StartWith("  \u276f "));
		Assert.That(model.Lines[1].Spans.Select(span => span.Role), Is.EqualTo(new[] { TextRole.Accent, TextRole.Accent, TextRole.Accent }));
		Assert.That(model.Lines[0].Spans.Select(span => span.Role), Is.EqualTo(new[] { TextRole.Primary, TextRole.Primary, TextRole.Primary }));
		Assert.That(model.Lines[1].Spans[1].Text, Is.EqualTo("/b"));
		Assert.That(model.Lines[1].Spans[2].Text, Is.EqualTo("two"));
	}

	[Test]
	public void KeyList_EmptyPage_ReturnsASingleMutedNotice()
	{
		var model = Layout().KeyList([], selectedIndex: 0);

		Assert.That(model.Kind, Is.EqualTo(PanelKind.Default));
		Assert.That(model.Lines.Single().Spans.Single().Role, Is.EqualTo(TextRole.Muted));
	}

	[Test]
	public void Search_WithoutQuery_ShowsPlaceholderAndCaret()
	{
		var model = Layout().Search(string.Empty);

		Assert.That(model.Lines.Single().Text, Does.Contain("Type to search"));
		Assert.That(model.Lines.Single().Text, Does.EndWith("\u2588"));
	}

	[Test]
	public void Search_WithQuery_KeepsTheQueryLiteral()
	{
		var model = Layout().Search("service/[a:b]");

		Assert.That(model.Lines.Single().Text, Does.Contain("service/[a:b]"));
		Assert.That(model.Lines.Single().Spans[1].Role, Is.EqualTo(TextRole.Primary));
	}

	[Test]
	public void BuildModel_WithoutSession_CarriesVersionOnly()
	{
		var footer = new FakeStatusBarRenderer();
		var statusBar = StatusBar(footer, new ConnectionSession());

		var model = statusBar.BuildModel();

		Assert.That(model.Version.Text, Is.EqualTo("0.0"));
		Assert.That(model.Version.Role, Is.EqualTo(TextRole.Primary));
		Assert.That(model.Name, Is.Null);
		Assert.That(model.Connection, Is.Null);
		Assert.That(model.Username, Is.Null);
	}

	[Test]
	public void BuildModel_WithAuthenticatedSession_AssignsRolesToSessionFields()
	{
		var footer = new FakeStatusBarRenderer();
		var session = new ConnectionSession();

		session.Start(new EtcdConnectionConfig
		{
			Name = "prod",
			ConnectionString = "http://localhost:2379",
			Username = "root"
		}, UserCapabilities.Unrestricted);

		var model = StatusBar(footer, session).BuildModel();

		Assert.That(model.Name?.Text, Is.EqualTo("prod"));
		Assert.That(model.Name?.Role, Is.EqualTo(TextRole.Secondary));
		Assert.That(model.Connection?.Text, Is.EqualTo("http://localhost:2379"));
		Assert.That(model.Username?.Text, Is.EqualTo("root"));
		Assert.That(model.Username?.Role, Is.EqualTo(TextRole.Warning));
	}

	[Test]
	public void BuildModel_WithUnauthenticatedSession_OmitsUsername()
	{
		var session = new ConnectionSession();

		session.Start(new EtcdConnectionConfig
		{
			Name = "prod",
			ConnectionString = "http://localhost:2379"
		}, UserCapabilities.Unrestricted);

		Assert.That(StatusBar(new FakeStatusBarRenderer(), session).BuildModel().Username, Is.Null);
	}

	[Test]
	public void BuildModel_MarksVersionExactlyOnce()
	{
		var model = StatusBar(new FakeStatusBarRenderer(), new ConnectionSession()).BuildModel();

		var versionSpans = model.Hints
			.Append(model.Version)
			.Where(span => span.Text.Contains("0.0"))
			.ToList();

		Assert.That(versionSpans, Has.Count.EqualTo(1));
	}

	[Test]
	public void BuildModel_HintsUsePrimaryForKeysAndMutedForDescriptions()
	{
		var hints = StatusBar(new FakeStatusBarRenderer(), new ConnectionSession()).BuildModel().Hints;

		Assert.That(hints.Select(span => span.Role), Is.EqualTo(new[]
		{
			TextRole.Primary,
			TextRole.Muted,
			TextRole.Primary,
			TextRole.Muted,
			TextRole.Primary,
			TextRole.Muted
		}));
	}

	[Test]
	public void BuildModel_WithCustomHints_UsesThemInsteadOfNavigationHints()
	{
		var statusBar = StatusBar(new FakeStatusBarRenderer(), new ConnectionSession());

		var model = statusBar.BuildModel([new StyledText("Press any key", TextRole.Muted)]);

		Assert.That(model.Hints.Single().Text, Is.EqualTo("Press any key"));
		Assert.That(model.Hints.Single().Role, Is.EqualTo(TextRole.Muted));
	}

	private static KeyBrowseLayout Layout() =>
		new(new FakeTerminal(), new EnglishLocalization());

	private static StatusBar StatusBar(FakeStatusBarRenderer footer, ConnectionSession session) =>
		new(new FakeTerminal(), new StubAppInfo(), session, new EnglishLocalization(), footer);

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
