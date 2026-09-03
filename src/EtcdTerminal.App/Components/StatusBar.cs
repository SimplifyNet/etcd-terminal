using EtcdTerminal.Terminal;
using EtcdTerminal.Configuration;
using Simplify.System;

namespace EtcdTerminal.App.Components;

public sealed class StatusBar(ITerminal _terminal)
{
	public void Render(EtcdConnectionConfig? config = null)
	{
		var left = $"{_terminal.Grey}  {_terminal.White}\u2191/\u2193{_terminal.Grey} navigate \u00b7 {_terminal.White}Enter{_terminal.Grey} confirm/select \u00b7 {_terminal.White}Esc{_terminal.Grey} back  ";
		var version = GetVersion();

		var rightPadding = "  ";

		string right;
		if (config is not null)
		{
			var connStr = config.ConnectionString.Length > 50
				? config.ConnectionString[..50] + "..."
				: config.ConnectionString;
			right = $"{_terminal.Green}\u2022{_terminal.Teal} {config.Name} {_terminal.Dim}\u00b7{_terminal.Grey} {connStr}";
			if (config.IsAuthenticationEnabled)
				right += $" {_terminal.Dim}\u00b7{_terminal.Yellow} {config.Username}";
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

	private static string GetVersion()
	{
		var version = AssemblyInfo.Entry.Version;

		return $"{version.Major}.{version.Minor}" + (version.Build != 0 ? "." + version.Build : "");
	}
}
