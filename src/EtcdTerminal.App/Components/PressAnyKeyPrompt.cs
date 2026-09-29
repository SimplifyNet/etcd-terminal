using EtcdTerminal.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

/// <summary>
/// Presents content as one complete screen: the banner, the body the caller
/// composed, and the session footer. The hint that a key continues belongs to
/// the footer, because the body is unbounded and a hint appended to it is the
/// first thing a short viewport crops. Holding the frame while waiting leaves a
/// single writer on every region of the screen.
/// </summary>
public sealed class PressAnyKeyPrompt(IScreenHost _host, Header _header, StatusBar _statusBar, ITerminalInput _input, ILocalization _localization)
{
	public void Show(IReadOnlyList<PanelModel> body)
	{
		_host.Begin(new ScreenModel
		{
			Header = _header.BuildModel(),
			Body = body,
			Footer = _statusBar.BuildModel(Hints())
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

	private IReadOnlyList<StyledText> Hints() =>
		[new StyledText(_localization.PressAnyKey, TextRole.Muted)];
}
