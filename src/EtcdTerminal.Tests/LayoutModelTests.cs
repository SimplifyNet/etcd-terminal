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
public sealed class LayoutModelTests
{
	[Test]
	public void Pagination_EmitsPageAndTotalsAsSeparateRoles()
	{
		var model = (TextBlock)Browse().Pagination(0, 5, 42, "total keys");
		var spans = model.Lines.Single();

		Assert.That(spans.Select(span => span.Text), Is.EqualTo(new[] { "Page ", "1/5", "  \u2022  ", "42", " total keys" }));
		Assert.That(spans.Select(span => span.Role), Is.EqualTo(new[]
		{
			TextRole.Muted,
			TextRole.Primary,
			TextRole.Muted,
			TextRole.Primary,
			TextRole.Muted
		}));
		Assert.That(model.Band, Is.True);
	}

	[Test]
	public void ActionPanel_WithModify_OffersSelectedKeyAndEveryAction()
	{
		var layout = Layout();

		var panel = (ActionPanelBlock)layout.ActionPanel("mykey", canModify: true);
		var selected = LineText.Of(panel.Title);
		var hints = LineText.Of(panel.Actions);

		Assert.That(selected, Is.EqualTo("Selected: mykey"));
		Assert.That(hints, Does.Contain("E Edit"));
		Assert.That(hints, Does.Contain("D Delete"));
		Assert.That(hints, Does.Contain("Esc Cancel"));
	}

	[Test]
	public void ActionPanel_WithoutModify_OffersOnlyCancel()
	{
		var panel = (ActionPanelBlock)Layout().ActionPanel("mykey", canModify: false);
		var hints = LineText.Of(panel.Actions);

		Assert.That(hints, Does.Contain("Esc Cancel"));
		Assert.That(hints, Does.Not.Contain("Edit"));
		Assert.That(hints, Does.Not.Contain("Delete"));
		Assert.That(panel.Title[1].Role, Is.EqualTo(TextRole.Accent));
	}

	[Test]
	public void ActionPanel_KeepsSelectedKeyLiteralAndNormalizesControls()
	{
		var literalPanel = (ActionPanelBlock)Layout().ActionPanel("service/[a:b]/\u043a\u043b\u044e\u0447", canModify: true);
		var sanitizedPanel = (ActionPanelBlock)Layout().ActionPanel("a\nb", canModify: true);

		Assert.That(LineText.Of(literalPanel.Title), Is.EqualTo("Selected: service/[a:b]/\u043a\u043b\u044e\u0447"));
		Assert.That(literalPanel.Title[1].Role, Is.EqualTo(TextRole.Accent));
		Assert.That(LineText.Of(sanitizedPanel.Title), Is.EqualTo("Selected: a\u23CEb"));
	}

	[Test]
	public void KeyList_MarksTheSelectedRowAndKeepsLiteralCells()
	{
		var keys = new[]
		{
			new EtcdKeyValue { Key = "/a", Value = "one" },
			new EtcdKeyValue { Key = "/b", Value = "two" }
		};
		var model = (TableBlock)Layout().KeyList(keys, selectedIndex: 1);

		Assert.That(model.Header, Is.Empty);
		Assert.That(model.IsFramed, Is.False);
		Assert.That(model.Pointer, Is.True, "the table carries the selection pointer column");
		Assert.That(model.Rows, Has.Count.EqualTo(2));
		Assert.That(model.Rows[0].Select(span => span.Role), Is.EqualTo(new[] { TextRole.Primary, TextRole.Primary, TextRole.Primary }));
		Assert.That(model.Rows[1].Select(span => span.Role), Is.EqualTo(new[] { TextRole.Accent, TextRole.Accent, TextRole.Accent }));
		Assert.That(model.Rows[0][0].Text, Is.EqualTo("  "));
		Assert.That(model.Rows[1][0].Text, Is.EqualTo("\u276f "));
		Assert.That(model.Rows[0][1].Text, Is.EqualTo("/a"));
		Assert.That(model.Rows[1][1].Text, Is.EqualTo("/b"));
		Assert.That(model.Rows[1][2].Text, Is.EqualTo("two"));
	}

	[Test]
	public void KeyList_EmptyPage_ReturnsASingleMutedNotice()
	{
		var model = (TextBlock)Layout().KeyList([], selectedIndex: 0);

		Assert.That(model.Lines, Has.Count.EqualTo(1));
		Assert.That(model.Lines.Single().Single().Role, Is.EqualTo(TextRole.Muted));
	}

	[Test]
	public void Search_WithoutQuery_ShowsPlaceholderAndCaret()
	{
		var model = (TextBlock)Browse().Search(string.Empty);
		var text = LineText.Of(model.Lines.Single());

		Assert.That(text, Does.Contain("Type to search"));
		Assert.That(text, Does.EndWith("\u2588"));
		Assert.That(text, Does.StartWith("\U0001f50d"), "no leading spaces before the magnifier");
		Assert.That(model.Band, Is.True);
	}

	[Test]
	public void Search_WithQuery_KeepsTheQueryLiteral()
	{
		var model = (TextBlock)Browse().Search("service/[a:b]");
		var line = model.Lines.Single();

		Assert.That(LineText.Of(line), Does.Contain("service/[a:b]"));
		Assert.That(line[1].Role, Is.EqualTo(TextRole.Primary));
		Assert.That(line[0].Text, Does.StartWith("\U0001f50d"), "no leading spaces before the magnifier");
		Assert.That(model.Band, Is.True);
	}

	[Test]
	public void BuildModel_WithoutSession_CarriesVersionOnly()
	{
		var statusBar = StatusBar(new ConnectionSession());

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
		var session = new ConnectionSession();

		session.Start(new EtcdConnectionConfig
		{
			Name = "prod",
			ConnectionString = "http://localhost:2379",
			Username = "root"
		}, UserCapabilities.Unrestricted);

		var model = StatusBar(session).BuildModel();

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

		Assert.That(StatusBar(session).BuildModel().Username, Is.Null);
	}

	[Test]
	public void BuildModel_MarksVersionExactlyOnce()
	{
		var model = StatusBar(new ConnectionSession()).BuildModel();

		var versionSpans = model.Hints
			.Append(model.Version)
			.Where(span => span.Text.Contains("0.0"))
			.ToList();

		Assert.That(versionSpans, Has.Count.EqualTo(1));
	}

	[Test]
	public void BuildModel_HintsUsePrimaryForKeysAndMutedForDescriptions()
	{
		var hints = StatusBar(new ConnectionSession()).BuildModel().Hints;

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

	private static KeyBrowseLayout Layout() =>
		new(new LocalizationCatalog());

	private static BrowseLayout Browse() =>
		new(new LocalizationCatalog());

	private static StatusBar StatusBar(ConnectionSession session) =>
		new(new StubAppInfo(), session, new LocalizationCatalog());

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
