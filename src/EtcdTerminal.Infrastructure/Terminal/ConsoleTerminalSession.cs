using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Theming;
using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// The only class in the application allowed to talk to <c>System.Console</c>
/// and to raw escape sequences. It splits the terminal once per session into a
/// scrolling viewport above the footer row and the footer row itself, so that
/// everything Spectre writes afterwards streams inside the viewport. A frame
/// that draws the footer itself temporarily takes the whole terminal and gives
/// the viewport split back when it is released.
/// </summary>
public sealed class ConsoleTerminalSession(IAnsiConsole _console, ITheme _theme) : ITerminalSession
{
	private const int FooterRows = 1;

	// The four sequences Spectre cannot emit (REFACTORING.md D1, D2).
	private const string ResetScrollRegionSequence = "\u001b[r";
	private const string ResetBackgroundSequence = "\u001b]111\u0007";

	public void Start()
	{
		Console.OutputEncoding = System.Text.Encoding.UTF8;

		if (!_console.Profile.Capabilities.Ansi)
			return;

		_console.WriteAnsi(writer =>
		{
			if (_console.Profile.Capabilities.AlternateBuffer)
				writer.EnterAltScreen();

			writer.Write(BackgroundSequence(_theme.WindowBackground));
			writer.HideCursor();
			writer.Write(ScrollRegionSequence(_console.Profile.Height - FooterRows));
			writer.EraseInDisplay(2);
			writer.CursorHome();
		});
	}

	public void Stop()
	{
		if (!_console.Profile.Capabilities.Ansi)
			return;

		_console.WriteAnsi(writer =>
		{
			writer.Write(ResetScrollRegionSequence);
			writer.Write(ResetBackgroundSequence);
			writer.EraseInDisplay(2);
			writer.CursorHome();
			writer.ShowCursor();

			if (_console.Profile.Capabilities.AlternateBuffer)
				writer.ExitAltScreen();
		});
	}

	public void BeginFrame() => SetScrollRegion(_console.Profile.Height);

	public void EndFrame() => SetScrollRegion(_console.Profile.Height - FooterRows);

	public void OnInterrupt(Action handler) => Console.CancelKeyPress += (_, args) =>
	{
		args.Cancel = true;
		handler();
	};

	private void SetScrollRegion(int lastRow)
	{
		if (!_console.Profile.Capabilities.Ansi)
			return;

		_console.WriteAnsi(writer => writer.Write(ScrollRegionSequence(lastRow)));
	}

	private static string ScrollRegionSequence(int lastRow) => $"\u001b[1;{lastRow}r";

	private static string BackgroundSequence(RgbColor c) => $"\u001b]11;#{c.R:X2}{c.G:X2}{c.B:X2}\u0007";
}
