using EtcdTerminal.Terminal;
using EtcdTerminal.Session;
using EtcdTerminal.Environment;
using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Components;

public sealed class StatusBar(ITerminalOutput _output, ITerminalCursor _cursor, ITerminalStyle _style, IAppInfo _appInfo, IConnectionSession _session)
{
	public const int ReservedRows = 3;

	public void EnsureCursorAboveBar(int rowsNeeded = 1)
	{
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

	public void Render()
	{
		var localization = LocalizationStore.Current;
		var active = _session.Active;
		var left = $"{_style.Muted}  {_style.Primary}\u2191/\u2193{_style.Muted} {localization.StatusNavigate} \u00b7 {_style.Primary}Enter{_style.Muted} {localization.StatusConfirm} \u00b7 {_style.Primary}Esc{_style.Muted} {localization.StatusBack}  ";
		var version = _appInfo.Version;
		var rightPadding = "  ";

		string right;

		if (active is not null)
		{
			var connStr = active.ConnectionString.Length > 50
				? active.ConnectionString[..50] + "..."
				: active.ConnectionString;

			right = $"{_style.Success}\u2022{_style.Secondary} {active.Name} {_style.Subtle}\u00b7{_style.Muted} {connStr}";
			if (active.IsAuthenticationEnabled)
				right += $" {_style.Subtle}\u00b7{_style.Warning} {active.Username}";
			right += $" {_style.Muted}v{_style.Primary}{version}";
		}
		else
			right = $"{_style.Muted}v{_style.Primary}{version}";

		var visibleWidth = _output.GetVisibleLength(left) + _output.GetVisibleLength(right) + rightPadding.Length;
		var pad = _output.WindowWidth - visibleWidth;

		if (pad < 0) pad = 0;

		var content = _style.PanelBackground + _style.Muted + left + new string(' ', pad) + right + _style.Muted + rightPadding + _style.Reset;

		_cursor.SetCursorPosition(0, _output.WindowHeight - 3);
		_output.Write(_output.FillRow(_style.PanelBackground));

		_cursor.SetCursorPosition(0, _output.WindowHeight - 2);
		_output.Write(content);

		_cursor.SetCursorPosition(0, _output.WindowHeight - 1);
		_output.Write(_output.FillRow(_style.PanelBackground));
	}
}
