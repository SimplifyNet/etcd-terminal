using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation.Terminal;
using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Components;

/// <summary>
/// Presents content as one complete screen: the banner, the body the caller
/// composed, the hint that a key continues, and the session footer. Holding the
/// frame while waiting keeps the hint above the footer and leaves a single
/// writer on every region of the screen.
/// </summary>
public sealed class PressAnyKeyPrompt(IScreenHost _host, Header _header, StatusBar _statusBar, ITerminalInput _input, ILocalization _localization)
{
	public void Show(IReadOnlyList<PanelModel> body)
	{
		_host.Begin(new ScreenModel
		{
			Header = _header.BuildModel(),
			Body = [.. body, Hint()],
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

	private PanelModel Hint() =>
		new(
		[
			new PanelLine([]),
			new PanelLine([new StyledText(_localization.PressAnyKey, TextRole.Muted)])
		]);
}
