using EtcdTerminal.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

/// <summary>
/// Presents an outcome as one complete screen: banner, message, the hint that a
/// key continues, and the footer. Composing the whole screen here keeps a single
/// writer on the footer and keeps the message from appending below a released
/// frame, which would scroll that frame's own footer back onto the screen.
/// </summary>
public sealed class Message(IScreenHost _host, Header _header, StatusBar _statusBar, ITerminalInput _input, ILocalization _localization)
{
	public void ShowSuccess(string text) => Show(text, TextRole.Success);

	public void ShowError(string text) => Show(text, TextRole.Danger);

	public void ShowWarning(string text) => Show(text, TextRole.Warning);

	public void ShowResult(bool ok, string success, string failure)
	{
		if (ok)
			ShowSuccess(success);
		else
			ShowError(failure);
	}

	private void Show(string text, TextRole role)
	{
		List<PanelLine> lines = [.. text.Split('\n').Select(line => new PanelLine([new StyledText(line.TrimEnd('\r'), role)]))];

		lines.Add(new PanelLine([]));
		lines.Add(new PanelLine([new StyledText(_localization.PressAnyKey, TextRole.Muted)]));

		_host.Begin(new ScreenModel
		{
			Header = _header.BuildModel(),
			Body = [new PanelModel(lines, PanelKind.Block)],
			Footer = _statusBar.BuildModel()
		});

		try
		{
			_input.ReadKey();
		}
		finally
		{
			_host.End();
		}
	}
}
