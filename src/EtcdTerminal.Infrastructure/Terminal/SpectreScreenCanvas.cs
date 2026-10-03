using EtcdTerminal.Presentation;
using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// The one writer of a screen while a session runs. The scroll region and the
/// footer row were reserved by <see cref="ConsoleTerminalSession"/>, so a
/// screen is opened by erasing the viewport, blocks stream through it and the
/// footer is redrawn in place on the row the scroll region never touches.
/// </summary>
public sealed class SpectreScreenCanvas(IAnsiConsole _console, BlockRenderer _blocks, StatusBarRenderer _footer) : IScreenCanvas
{
	public void NewScreen(StatusBarModel footer)
	{
		_console.Clear(true);

		if (_console.Profile.Capabilities.Ansi)
			UpdateFooter(footer);
		else
			_console.Write(_footer.Build(footer));
	}

	public void Write(Block block) => _console.Write(_blocks.Render(block));

	public void Write(IReadOnlyList<Block> blocks)
	{
		foreach (var block in blocks)
			Write(block);
	}

	public void UpdateFooter(StatusBarModel footer)
	{
		if (!_console.Profile.Capabilities.Ansi)
			return;

		var renderable = _footer.Build(footer);

		_console.WriteAnsi(writer => writer.SaveCursor(false).CursorPosition(_console.Profile.Height, 1));
		_console.Write(renderable);
		_console.WriteAnsi(writer => writer.RestoreCursor(false));
	}

	public void WriteException(Exception exception) => _console.WriteException(exception);
}
