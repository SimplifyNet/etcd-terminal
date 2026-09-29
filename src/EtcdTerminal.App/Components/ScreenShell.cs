using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Components;

/// <summary>
/// Opens a screen that has no framed content of its own: the banner and the
/// session footer, composed and released by the one host that owns the console.
/// Prompts, spinners and tables stream underneath it, so the frame is given back
/// at once instead of staying live while the screen writes inline.
/// </summary>
public sealed class ScreenShell(IScreenHost _host, Header _header, StatusBar _statusBar)
{
	public void Show()
	{
		_host.Begin(new ScreenModel
		{
			Header = _header.BuildModel(),
			Footer = _statusBar.BuildModel()
		});

		_host.End();
	}
}
