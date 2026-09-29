using EtcdTerminal.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

/// <summary>
/// Presents an outcome as one complete screen: the banner, the message, and
/// the session footer. The hint that a key continues belongs to the footer, so
/// a screen with an unbounded body still tells the user what to do when the
/// viewport is short. Composing the whole screen here keeps a single writer on
/// the footer and keeps the message from appending below a released frame,
/// which would scroll that frame's own footer back onto the screen.
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

		_host.Begin(new ScreenModel
		{
			Header = _header.BuildModel(),
			Body = [new PanelModel(lines)],
			Footer = _statusBar.BuildModel(
			[
				new StyledText(_localization.PressAnyKey, TextRole.Muted)
			])
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
