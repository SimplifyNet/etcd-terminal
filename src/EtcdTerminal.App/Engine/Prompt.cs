using System.Diagnostics;
using System.Text;
using EtcdTerminal.Configuration;
using EtcdTerminal.Infrastructure.Terminal;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;
using Spectre.Console;

namespace EtcdTerminal.App.Engine;

public sealed class Prompt(ITerminal _terminal)
{
	private const int _pasteBurstThresholdMs = 40;

	private static readonly Style _promptStyle = new(decoration: Decoration.Bold);

	private readonly IAnsiConsole _console = new EscapableConsole(AnsiConsole.Console);

	public string? Ask(string prompt, bool allowEmpty = false)
	{
		_terminal.SetCursorVisible(true);

		try
		{
			_terminal.Write(_terminal.SelectionPointerEmpty);

			var input = _console.Prompt(new TextPrompt<string>(prompt)
				.PromptStyle(_promptStyle)
				.AllowEmpty());

			if (AppSettingsStore.Current.TrimInputValues)
				input = input.Trim();

			if (string.IsNullOrWhiteSpace(input))
				return allowEmpty ? string.Empty : null;

			return input;
		}
		catch (OperationCanceledException)
		{
			return null;
		}
		finally
		{
			_terminal.SetCursorVisible(false);
		}
	}

	public string? Ask(string prompt, string defaultValue)
	{
		_terminal.SetCursorVisible(true);

		try
		{
			_terminal.Write(_terminal.SelectionPointerEmpty);

			var input = _console.Prompt(new TextPrompt<string>(prompt)
				.PromptStyle(_promptStyle)
				.AllowEmpty()
				.DefaultValue(defaultValue)
				.EditableDefaultValue(true)
				.ShowDefaultValue(false));

			return AppSettingsStore.Current.TrimInputValues ? input.Trim() : input;
		}
		catch (OperationCanceledException)
		{
			return null;
		}
		finally
		{
			_terminal.SetCursorVisible(false);
		}
	}

	public string? Secret(string prompt)
	{
		_terminal.SetCursorVisible(true);

		try
		{
			_terminal.Write(_terminal.SelectionPointerEmpty);

			return _console.Prompt(new TextPrompt<string>(prompt)
				.PromptStyle(_promptStyle)
				.Secret()
				.AllowEmpty());
		}
		catch (OperationCanceledException)
		{
			return null;
		}
		finally
		{
			_terminal.SetCursorVisible(false);
		}
	}

	public async Task<string?> ReadMultiLineAsync(string prompt)
	{
		_terminal.SetCursorVisible(false);

		try
		{
			_terminal.Write(_terminal.SelectionPointerEmpty + prompt + " ");
			_terminal.WriteLine();
			_terminal.WriteLine();
			_terminal.Flush();

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
			_terminal.SetCursorVisible(false);
		}
	}

	private static int CountLines(StringBuilder buffer)
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
			? LocalizationStore.Current.WaitingForPaste
			: string.Format(LocalizationStore.Current.PastedLines, lines);

		var color = lines == 0 ? _terminal.Dim : _terminal.Accent;

		_terminal.Write("\r" + new string(' ', Math.Max(0, _terminal.WindowWidth - 1)));
		_terminal.Write("\r" + _terminal.Indent + color + text + _terminal.Reset);
		_terminal.Flush();
	}
}
