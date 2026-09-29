using EtcdTerminal.Terminal;
using EtcdTerminal.Session;
using EtcdTerminal.Environment;
using EtcdTerminal.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Configuration;

namespace EtcdTerminal.App.Components;

public sealed class StatusBar(ITerminalOutput _output, ITerminalCursor _cursor, IAppInfo _appInfo, IConnectionSession _session, ILocalization _localization, IStatusBarRenderer _statusBar)
{
	public const int ReservedRows = 3;

	public void EnsureCursorAboveBar(int rowsNeeded = 1)
	{
		if (_output.WindowHeight < ReservedRows + rowsNeeded)
			return;

		var lastContentRow = _output.WindowHeight - ReservedRows - rowsNeeded;
		var overflow = _cursor.CursorTop - lastContentRow;

		if (overflow <= 0)
			return;

		var newLines = _output.WindowHeight - 1 - _cursor.CursorTop + overflow;

		for (var i = 0; i < newLines; i++)
			_output.WriteLine();

		_cursor.SetCursorPosition(0, lastContentRow);
	}

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
	/// width and does not decide what to drop. A screen that is not a menu
	/// passes its own hints, because navigation hints are wrong there.
	/// </summary>
	public StatusBarModel BuildModel(IReadOnlyList<StyledText>? hints = null)
	{
		var active = _session.Active;

		return new StatusBarModel
		{
			Hints = hints ?? BuildHints(),
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
