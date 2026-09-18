using System.Diagnostics;
using System.Text;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

public sealed class MultiLinePasteReader(ITerminal _terminal, StatusBar _statusBar, ILocalization _localization)
{
	private const int _pasteBurstThresholdMs = 40;

	public async Task<string?> ReadAsync(string prompt)
	{
		_terminal.SetCursorVisible(false);

		try
		{
			_statusBar.EnsureCursorAboveBar();
			_terminal.Write(_terminal.Indent + prompt + " ");
			_terminal.WriteLine();
			_terminal.WriteLine();
			_terminal.Flush();

			_statusBar.RenderPreservingCursor();

			while (_terminal.KeyAvailable)
				_terminal.ReadKey();

			var buffer = new StringBuilder();
			var lastKeyAt = Stopwatch.StartNew();

			RenderPasteStatus(0);

			while (true)
			{
				var key = _terminal.ReadKey();
				var elapsed = lastKeyAt.ElapsedMilliseconds;

				lastKeyAt.Restart();

				if (key.Key == ConsoleKey.Escape)
				{
					_terminal.WriteLine();

					return null;
				}

				if (key.Key == ConsoleKey.Enter)
				{
					if (buffer.Length > 0 && !await IsPastedNewLineAsync(elapsed))
						break;

					buffer.Append('\n');
				}
				else if (key.Key is ConsoleKey.Backspace or ConsoleKey.Delete)
					buffer.Clear();
				else if (key.KeyChar == '\t')
					buffer.Append('\t');
				else if (!char.IsControl(key.KeyChar))
					buffer.Append(key.KeyChar);

				if (!_terminal.KeyAvailable)
					RenderPasteStatus(CountLines(buffer));
			}

			_terminal.WriteLine();

			var text = buffer.ToString();

			return string.IsNullOrWhiteSpace(text) ? null : text;
		}
		finally
		{
			_statusBar.RenderPreservingCursor();
			_terminal.SetCursorVisible(false);
		}
	}

	internal static int CountLines(StringBuilder buffer)
	{
		var lines = 0;
		var lineHasContent = false;

		for (var i = 0; i < buffer.Length; i++)
		{
			if (buffer[i] == '\n')
			{
				if (lineHasContent)
					lines++;

				lineHasContent = false;
			}
			else if (!char.IsWhiteSpace(buffer[i]))
				lineHasContent = true;
		}

		return lineHasContent ? lines + 1 : lines;
	}

	private async Task<bool> IsPastedNewLineAsync(long elapsedSinceLastKey)
	{
		if (elapsedSinceLastKey < _pasteBurstThresholdMs)
			return true;

		if (_terminal.KeyAvailable)
			return true;

		await Task.Delay(_pasteBurstThresholdMs);

		return _terminal.KeyAvailable;
	}

	private void RenderPasteStatus(int lines)
	{
		var text = lines == 0
			? _localization.WaitingForPaste
			: string.Format(_localization.PastedLines, lines);

		var color = lines == 0 ? _terminal.Subtle : _terminal.Accent;

		_terminal.Write("\r" + new string(' ', Math.Max(0, _terminal.WindowWidth - 1)));
		_terminal.Write("\r" + _terminal.Indent + color + text + _terminal.Reset);
		_terminal.Flush();
	}
}
