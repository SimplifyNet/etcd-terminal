using EtcdTerminal.Terminal;
using EtcdTerminal.App.Components;

namespace EtcdTerminal.App.Engine;

public sealed class Menu(ITerminal _terminal, StatusBar _statusBar)
{

	public MenuItem<TId>? Show<TId>(string title, IReadOnlyList<MenuItem<TId>> items, Func<string, string>? displayConverter = null)
	{
		var index = ShowAndGetIndex(title, items, displayConverter);

		if (index is null)
			return null;

		return items[index.Value];
	}

	private int? ShowAndGetIndex<TId>(string title, IReadOnlyList<MenuItem<TId>> items, Func<string, string>? displayConverter)
	{
		var selectable = items.Select(i => i.IsSelectable).ToList();
		var index = selectable.FindIndex(s => s);

		if (index < 0)
			return null;

		var plain = items.Select(i => displayConverter?.Invoke(i.Label) ?? i.Label).ToList();

		var menuStart = _terminal.CursorTop;

		_terminal.ResetColor();

		if (!string.IsNullOrEmpty(title))
		{
			_terminal.WriteLine();
			_terminal.WriteLine($"{_terminal.Indent}{title}");
			_terminal.WriteLine();
		}

		var firstItemTop = _terminal.CursorTop;

		for (var i = 0; i < items.Count; i++)
			DrawItem(firstItemTop + i, plain[i], i == index);

		var menuEnd = _terminal.CursorTop;

		_statusBar.Render();
		_terminal.SetCursorPosition(0, menuEnd);

		while (true)
		{
			var key = _terminal.ReadKey();
			var oldIndex = index;

			switch (key.Key)
			{
				case ConsoleKey.Escape:
					ClearMenu(menuStart);
					return null;
				case ConsoleKey.Enter:
					if (!selectable[index])
						continue;

					ClearMenu(menuStart);
					return index;
				case ConsoleKey.UpArrow:
					index = StepSelection(index, -1);
					break;
				case ConsoleKey.DownArrow:
					index = StepSelection(index, 1);
					break;
				default:
					continue;
			}

			DrawItem(firstItemTop + oldIndex, plain[oldIndex], false);
			DrawItem(firstItemTop + index, plain[index], true);
		}

		int StepSelection(int from, int direction)
		{
			var next = from;

			for (var n = 0; n < items.Count; n++)
			{
				next = (next + direction + items.Count) % items.Count;

				if (selectable[next])
					return next;
			}

			return from;
		}
	}

	private void ClearMenu(int menuStart)
	{
		_terminal.SetCursorPosition(0, menuStart);
		_terminal.ClearToEndOfScreen();
	}

	private void DrawItem(int top, string text, bool isSelected)
	{
		_terminal.SetCursorPosition(0, top);
		_terminal.ResetColor();

		_terminal.Write(isSelected ? _terminal.Accent : string.Empty);
		_terminal.Write(isSelected ? _terminal.SelectionPointer : _terminal.Indent);

		WriteTruncated(text);
		_terminal.ResetColor();
	}

	private void WriteTruncated(string text)
	{
		var maxLen = _terminal.WindowWidth - _terminal.SelectionPointer.Length - 1;

		if (text.Length > maxLen)
		{
			_terminal.Write(text.AsSpan(0, Math.Max(0, maxLen - 3)).ToString());
			_terminal.Write("...");
		}
		else
			_terminal.Write(text);
	}
}
