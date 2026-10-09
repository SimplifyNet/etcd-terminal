using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Theming;
using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// The only class in the application allowed to talk to <c>System.Console</c>
/// and to raw escape sequences. It splits the terminal once per session into a
/// scrolling viewport above the footer rows and the footer rows themselves, so
/// that everything Spectre writes afterwards streams inside the viewport.
/// It also watches the window size: when it changes, the split is redone
/// for the new height and the resize handlers run.
/// </summary>
public sealed class ConsoleTerminalSession(IAnsiConsole _console, IThemeCatalog _themes) : ITerminalSession
{
	private const int FooterRows = 3;

	private static readonly TimeSpan SizePollInterval = TimeSpan.FromMilliseconds(200);

	private readonly List<Action> _resizeHandlers = [];

	private Timer? _sizeWatch;
	private (int Width, int Height) _size;

	// The six sequences Spectre cannot emit. Alternate scroll (DECSET 1007)
	// makes the terminal report the mouse wheel as arrow keys inside the
	// alternate buffer, which is how a page gets scrolled by the wheel.
	private const string ResetScrollRegionSequence = "\u001b[r";
	private const string ResetBackgroundSequence = "\u001b]111\u0007";
	private const string AlternateScrollOnSequence = "\u001b[?1007h";
	private const string AlternateScrollOffSequence = "\u001b[?1007l";

	public void Start()
	{
		_themes.Changed += UpdateBackground;

		Console.OutputEncoding = System.Text.Encoding.UTF8;

		if (!_console.Profile.Capabilities.Ansi)
			return;

		ConfigureTerminal();

		_size = CurrentSize();
		_sizeWatch = new(_ => CheckSize(), null, SizePollInterval, SizePollInterval);
	}

	public void Stop()
	{
		_themes.Changed -= UpdateBackground;
		_sizeWatch?.Dispose();

		if (!_console.Profile.Capabilities.Ansi)
			return;

		RestoreTerminal();
	}

	public void OnInterrupt(Action handler) => Console.CancelKeyPress += (_, args) =>
	{
		args.Cancel = true;
		handler();
	};

	public void OnResize(Action handler) => _resizeHandlers.Add(handler);

	private (int Width, int Height) CurrentSize() => (_console.Profile.Width, _console.Profile.Height);

	private void ConfigureTerminal() => _console.WriteAnsi(writer =>
	{
		if (_console.Profile.Capabilities.AlternateBuffer)
			writer.EnterAltScreen();

		writer.Write(AlternateScrollOnSequence);
		writer.Write(BackgroundSequence(_themes.Current.WindowBackground));
		writer.HideCursor();
		writer.Write(ScrollRegionSequence(_console.Profile.Height - FooterRows));
		writer.EraseInDisplay(2);
		writer.CursorHome();
	});

	private void RestoreTerminal() => _console.WriteAnsi(writer =>
	{
		writer.Write(ResetScrollRegionSequence);
		writer.Write(AlternateScrollOffSequence);
		writer.Write(ResetBackgroundSequence);
		writer.EraseInDisplay(2);
		writer.CursorHome();
		writer.ShowCursor();

		if (_console.Profile.Capabilities.AlternateBuffer)
			writer.ExitAltScreen();
	});

	private void UpdateBackground()
	{
		if (_console.Profile.Capabilities.Ansi)
			_console.WriteAnsi(writer => writer.Write(BackgroundSequence(_themes.Current.WindowBackground)));

		NotifyResizeHandlers();
	}

	/// The scroll region is bound to the old height and would overlap the
	/// footer rows, so the terminal is split again before the handlers draw.
	private void CheckSize()
	{
		var size = CurrentSize();

		if (size == _size)
			return;

		_size = size;

		ConfigureViewport(size.Height);

		NotifyResizeHandlers();
	}

	private void ConfigureViewport(int height) => _console.WriteAnsi(writer =>
	{
		writer.Write(ScrollRegionSequence(height - FooterRows));
		writer.EraseInDisplay(2);
		writer.CursorHome();
	});

	private void NotifyResizeHandlers()
	{
		foreach (var handler in _resizeHandlers)
			handler();
	}

	private static string ScrollRegionSequence(int lastRow) => $"\u001b[1;{lastRow}r";

	private static string BackgroundSequence(RgbColor c) => $"\u001b]11;#{c.R:X2}{c.G:X2}{c.B:X2}\u0007";
}
