using EtcdTerminal.Terminal;
using EtcdTerminal.Session;
using EtcdTerminal.Environment;
using EtcdTerminal.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Configuration;

namespace EtcdTerminal.App.Components;

public sealed class StatusBar(ITerminalCursor _cursor, IAppInfo _appInfo, IConnectionSession _session, ILocalization _localization, IStatusBarRenderer _statusBar)
{
	public void EnsureRoomAbove(int rows = 1) => _statusBar.EnsureRoomAbove(rows);

	public void ClearBelow() => _statusBar.ClearBelow();

	public void RenderPreservingCursor()
	{
		var left = _cursor.CursorLeft;
		var top = _cursor.CursorTop;

		Render();
		_cursor.SetCursorPosition(left, top);
	}

	public void Render() => _statusBar.Write(BuildModel());

	/// <summary>
	/// Collects session information and localized hints. This component owns the
	/// literal text and the role of every field; it does not know the available
	/// width and does not decide what to drop.
	/// </summary>
	public StatusBarModel BuildModel()
	{
		var active = _session.Active;

		return new StatusBarModel
		{
			Hints = BuildHints(),
			Name = active is null ? null : new StyledText(ValuePreview.Sanitize(active.Name), TextRole.Secondary),
			Connection = active is null ? null : new StyledText(ValuePreview.Sanitize(active.ConnectionString), TextRole.Muted),
			Username = Username(active),
			Version = new StyledText(_appInfo.Version, TextRole.Primary)
		};
	}

	private StyledText? Username(EtcdConnectionConfig? active) =>
		active is not null && active.IsAuthenticationEnabled && active.Username is not null
			? new StyledText(ValuePreview.Sanitize(active.Username), TextRole.Warning)
			: null;

	private IReadOnlyList<StyledText> BuildHints() =>
	[
		new StyledText("\u2191/\u2193", TextRole.Primary),
		new StyledText($" {_localization.StatusNavigate} \u00b7 ", TextRole.Muted),
		new StyledText("Enter", TextRole.Primary),
		new StyledText($" {_localization.StatusConfirm} \u00b7 ", TextRole.Muted),
		new StyledText("Esc", TextRole.Primary),
		new StyledText($" {_localization.StatusBack}", TextRole.Muted)
	];
}
