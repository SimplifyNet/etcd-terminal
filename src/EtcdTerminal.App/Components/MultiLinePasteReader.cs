using System.Diagnostics;
using System.Text;
using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Components;

public sealed class MultiLinePasteReader(IKeyReader _keys, ILiveFrame _live, Screen _screen, ILocalization _localization)
{
	private const int _pasteBurstThresholdMs = 40;

	public Task<string?> ReadAsync(string prompt)
	{
		_screen.Write(
		[
			TextBlock.Line(new StyledText(prompt)),
			TextBlock.Blank()
		]);

		var text = _live.Run<string?>(Status(0), LiveFrameEnd.Keep, ReadPaste);

		return Task.FromResult(text);
	}

	private string? ReadPaste(ILiveFrameUpdater updater)
	{
		// Previously queued input (a paste or an Escape that cancels) is
		// consumed below, never discarded here.
		var buffer = new StringBuilder();
		var lastKeyAt = Stopwatch.StartNew();

		while (true)
		{
			var key = _keys.ReadKey();
			var elapsed = lastKeyAt.ElapsedMilliseconds;

			lastKeyAt.Restart();

			if (key.Key == ConsoleKey.Escape)
				return null;

			if (key.Key == ConsoleKey.Enter)
			{
				if (buffer.Length > 0 && !IsPastedNewLine(elapsed))
					break;

				buffer.Append('\n');
			}
			else if (key.Key is ConsoleKey.Backspace or ConsoleKey.Delete)
				buffer.Clear();
			else if (key.KeyChar == '\t')
				buffer.Append('\t');
			else if (!char.IsControl(key.KeyChar))
				buffer.Append(key.KeyChar);

			if (!_keys.KeyAvailable)
				updater.Update(Status(CountLines(buffer)));
		}

		var pasted = buffer.ToString();

		return string.IsNullOrWhiteSpace(pasted) ? null : pasted;
	}

	internal static int CountLines(string text) => CountLines(new StringBuilder(text));

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

	private bool IsPastedNewLine(long elapsedSinceLastKey)
	{
		if (elapsedSinceLastKey < _pasteBurstThresholdMs)
			return true;

		if (_keys.KeyAvailable)
			return true;

		Thread.Sleep(_pasteBurstThresholdMs);

		return _keys.KeyAvailable;
	}

	private FrameModel Status(int lines)
	{
		var text = lines == 0
			? _localization.WaitingForPaste
			: string.Format(_localization.PastedLines, lines);

		return new([TextBlock.Line(new StyledText(text, lines == 0 ? TextRole.Subtle : TextRole.Accent))]);
	}
}
