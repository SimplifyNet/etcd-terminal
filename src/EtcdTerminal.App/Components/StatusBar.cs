using EtcdTerminal.Terminal;
using EtcdTerminal.Session;
using EtcdTerminal.Environment;
using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Components;

public sealed class StatusBar(ITerminal _terminal, IAppInfo _appInfo, IConnectionSession _session)
{
	public const int ReservedRows = 3;

	public void EnsureCursorAboveBar(int rowsNeeded = 1)
	{
		var lastContentRow = _terminal.WindowHeight - ReservedRows - rowsNeeded;
		var overflow = _terminal.CursorTop - lastContentRow;

		if (overflow <= 0)
			return;

		var newLines = _terminal.WindowHeight - 1 - _terminal.CursorTop + overflow;

		for (var i = 0; i < newLines; i++)
			_terminal.WriteLine();

		_terminal.SetCursorPosition(0, lastContentRow);
	}

	public void RenderPreservingCursor()
	{
		var left = _terminal.CursorLeft;
		var top = _terminal.CursorTop;

		Render();
		_terminal.SetCursorPosition(left, top);
	}

	public void Render()
	{
		var localization = LocalizationStore.Current;
		var active = _session.Active;
		var left = $"{_terminal.Grey}  {_terminal.White}\u2191/\u2193{_terminal.Grey} {localization.StatusNavigate} \u00b7 {_terminal.White}Enter{_terminal.Grey} {localization.StatusConfirm} \u00b7 {_terminal.White}Esc{_terminal.Grey} {localization.StatusBack}  ";
		var version = _appInfo.Version;
		var rightPadding = "  ";

		string right;

		if (active is not null)
		{
			var connStr = active.ConnectionString.Length > 50
				? active.ConnectionString[..50] + "..."
				: active.ConnectionString;

			right = $"{_terminal.Green}\u2022{_terminal.Teal} {active.Name} {_terminal.Dim}\u00b7{_terminal.Grey} {connStr}";
			if (active.IsAuthenticationEnabled)
				right += $" {_terminal.Dim}\u00b7{_terminal.Yellow} {active.Username}";
			right += $" {_terminal.Grey}v{_terminal.White}{version}";
		}
		else
			right = $"{_terminal.Grey}v{_terminal.White}{version}";

		var visibleWidth = _terminal.GetVisibleLength(left) + _terminal.GetVisibleLength(right) + rightPadding.Length;
		var pad = _terminal.WindowWidth - visibleWidth;

		if (pad < 0) pad = 0;

		var content = _terminal.Bg + _terminal.Grey + left + new string(' ', pad) + right + _terminal.Grey + rightPadding + _terminal.Reset;

		_terminal.SetCursorPosition(0, _terminal.WindowHeight - 3);
		_terminal.Write(_terminal.FillRow(_terminal.Bg));

		_terminal.SetCursorPosition(0, _terminal.WindowHeight - 2);
		_terminal.Write(content);

		_terminal.SetCursorPosition(0, _terminal.WindowHeight - 1);
		_terminal.Write(_terminal.FillRow(_terminal.Bg));
	}
}
