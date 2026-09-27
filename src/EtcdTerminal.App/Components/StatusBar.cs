using EtcdTerminal.Terminal;
using EtcdTerminal.Session;
using EtcdTerminal.Environment;
using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Components;

public sealed class StatusBar(ITerminalOutput _output, ITerminalCursor _cursor, ITerminalStyle _style, IAppInfo _appInfo, IConnectionSession _session, ILocalization _localization)
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

	public void Render()
	{
		if (_output.WindowHeight < ReservedRows)
			return;

		var width = _output.WindowWidth;
		var active = _session.Active;
		var connectionString = active?.ConnectionString;
		var version = _appInfo.Version;
		var rightPadding = "  ";
		var left = $"{_style.Muted}  {_style.Primary}\u2191/\u2193{_style.Muted} {_localization.StatusNavigate} \u00b7 {_style.Primary}Enter{_style.Muted} {_localization.StatusConfirm} \u00b7 {_style.Primary}Esc{_style.Muted} {_localization.StatusBack}  ";

		string? name = null;
		string? connection = null;
		string? username = null;

		if (active is not null)
		{
			name = ValuePreview.Preview(active.Name, 24);
			connection = ValuePreview.Preview(active.ConnectionString, 50);

			if (active.IsAuthenticationEnabled && active.Username is not null)
				username = ValuePreview.Preview(active.Username, 24);
		}

		var right = BuildRight(name, connection, username, version);

		if (!Fits(left, right, rightPadding, width) && connectionString is not null)
		{
			connection = ValuePreview.Preview(connectionString, 20);
			right = BuildRight(name, connection, username, version);
		}

		if (!Fits(left, right, rightPadding, width))
		{
			username = null;
			right = BuildRight(name, connection, username, version);
		}

		if (!Fits(left, right, rightPadding, width))
		{
			connection = null;
			right = BuildRight(name, connection, username, version);
		}

		if (!Fits(left, right, rightPadding, width))
		{
			left = string.Empty;
			right = BuildRight(name, connection, username, version);
		}

		if (!Fits(left, right, rightPadding, width) && active is not null)
		{
			var reserved = _output.GetVisibleLength($"{_style.Success}\u2022{_style.Secondary}  {_style.Muted}v{_style.Primary}{version}") + rightPadding.Length;
			var nameBudget = width - reserved;

			name = nameBudget <= 0 ? null : ValuePreview.Preview(active.Name, nameBudget);
			right = BuildRight(name, null, null, version);
		}

		var pad = width - _output.GetVisibleLength(left) - _output.GetVisibleLength(right) - rightPadding.Length;

		if (pad < 0) pad = 0;

		var content = _style.PanelBackground + _style.Muted + left + new string(' ', pad) + right + _style.Muted + rightPadding + _style.Reset;

		_cursor.SetCursorPosition(0, _output.WindowHeight - 3);
		_output.Write(_output.FillRow(_style.PanelBackground));

		_cursor.SetCursorPosition(0, _output.WindowHeight - 2);
		_output.Write(content);

		_cursor.SetCursorPosition(0, _output.WindowHeight - 1);
		_output.Write(_output.FillRow(_style.PanelBackground));
	}

	private bool Fits(string left, string right, string rightPadding, int width) =>
		_output.GetVisibleLength(left) + _output.GetVisibleLength(right) + rightPadding.Length <= width;

	private string BuildRight(string? name, string? connection, string? username, string version)
	{
		if (name is null)
			return $"{_style.Muted}v{_style.Primary}{version}";

		var right = $"{_style.Success}\u2022{_style.Secondary} {name}";

		if (connection is not null)
			right += $" {_style.Subtle}\u00b7{_style.Muted} {connection}";

		if (username is not null)
			right += $" {_style.Subtle}\u00b7{_style.Warning} {username}";

		return right + $" {_style.Muted}v{_style.Primary}{version}";
	}
}
