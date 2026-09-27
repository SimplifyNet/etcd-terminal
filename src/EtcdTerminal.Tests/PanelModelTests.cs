using EtcdTerminal.App.Components;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens.Keys;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
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
	public void RenderPagination_EmitsPageAndTotalsAsSeparateRoles()
	{
		var panels = new FakePanelRenderer();
		var layout = Layout(panels);

		layout.RenderPagination(0, 5, 42);

		var model = panels.Last;
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
	public void RenderActionBar_WithModify_OffersEditDeleteAndCancel()
	{
		var panels = new FakePanelRenderer();
		var layout = Layout(panels);

		layout.RenderActionBar("mykey", canModify: true);

		Assert.That(panels.Models.Select(model => model.Kind), Is.EqualTo(new[] { PanelKind.Selection, PanelKind.Actions }));

		var hints = panels.Last.Lines.Single().Text;

		Assert.That(hints, Does.Contain("E Edit"));
		Assert.That(hints, Does.Contain("D Delete"));
		Assert.That(hints, Does.Contain("Esc Cancel"));
	}

	[Test]
	public void RenderActionBar_WithoutModify_OffersOnlyCancel()
	{
		var panels = new FakePanelRenderer();

		Layout(panels).RenderActionBar("mykey", canModify: false);

		var hints = panels.Last.Lines.Single().Text;

		Assert.That(hints, Does.Contain("Esc Cancel"));
		Assert.That(hints, Does.Not.Contain("Edit"));
		Assert.That(hints, Does.Not.Contain("Delete"));
	}

	[Test]
	public void RenderActionBar_KeepsSelectedKeyLiteral()
	{
		var panels = new FakePanelRenderer();

		Layout(panels).RenderActionBar("service/[a:b]/ключ", canModify: true);

		var selection = panels.Models[0];

		Assert.That(selection.Lines.Single().Text, Is.EqualTo("Selected: service/[a:b]/ключ"));
		Assert.That(selection.Lines.Single().Spans[1].Role, Is.EqualTo(TextRole.Accent));
	}

	[Test]
	public void RenderActionBar_NormalizesControlCharactersInSelectedKey()
	{
		var panels = new FakePanelRenderer();

		Layout(panels).RenderActionBar("a\nb", canModify: true);

		Assert.That(panels.Models[0].Lines.Single().Text, Is.EqualTo("Selected: a\u23CEb"));
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

	private static KeyBrowseLayout Layout(FakePanelRenderer panels) =>
		new(new FakeTerminal(), new FakeTerminal(), new FakeTerminal(), new EnglishLocalization(), panels);

	private static StatusBar StatusBar(FakeStatusBarRenderer footer, ConnectionSession session) =>
		new(new FakeTerminal(), new FakeTerminal(), new StubAppInfo(), session, new EnglishLocalization(), footer);

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
