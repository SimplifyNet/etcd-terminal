using EtcdTerminal.Session;
using EtcdTerminal.Environment;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Configuration;

namespace EtcdTerminal.App.Components;

/// <summary>
/// The session footer's model: the localized keyboard hints on the left and the
/// connection details on the right. This component owns the literal text and
/// the role of every field; it does not know the available width and does not
/// decide what to drop — <c>StatusBarFallbacks</c> owns that priority order.
/// </summary>
public sealed class StatusBar(IAppInfo _appInfo, IConnectionSession _session, ILocalizationCatalog _localizations)
{
	public StatusBarModel BuildModel()
	{
		var active = _session.Active;

		return new StatusBarModel
		{
			Hints = BuildHints(),
			Name = active is null ? null : new StyledText(DisplayText.Sanitize(active.Name), TextRole.Secondary),
			Connection = active is null ? null : new StyledText(DisplayText.Sanitize(active.ConnectionString), TextRole.Muted),
			Username = Username(active),
			Version = new StyledText(_appInfo.Version, TextRole.Primary)
		};
	}

	private StyledText? Username(EtcdConnectionConfig? active) =>
		active is not null && active.IsAuthenticationEnabled && active.Username is not null
			? new StyledText(DisplayText.Sanitize(active.Username), TextRole.Warning)
			: null;

	private IReadOnlyList<StyledText> BuildHints() =>
	[
		new StyledText("\u2191/\u2193", TextRole.Primary),
		new StyledText($" {_localizations.Current.StatusNavigate} \u00b7 ", TextRole.Muted),
		new StyledText("Enter", TextRole.Primary),
		new StyledText($" {_localizations.Current.StatusConfirm} \u00b7 ", TextRole.Muted),
		new StyledText("Esc", TextRole.Primary),
		new StyledText($" {_localizations.Current.StatusBack}", TextRole.Muted)
	];
}
