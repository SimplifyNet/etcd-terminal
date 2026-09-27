using EtcdTerminal.Terminal;
using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Engine;

/// <summary>
/// Selection over a list of items with stable identifiers. A screen that owns the
/// whole console uses <see cref="ShowFramed"/>, where the menu is the only writer
/// and the frame is replaced in place. A confirmation shown in the middle of
/// other output uses <see cref="Show"/>, which draws inline.
/// </summary>
public sealed class Menu(ITerminal _terminal, ITerminalInput _input, IScreenHost _host, Header _header, StatusBar _statusBar)
{
	public MenuItem<TId>? Show<TId>(string title, IReadOnlyList<MenuItem<TId>> items, Func<string, string>? displayConverter = null)
	{
		var index = ShowAndGetIndex(title, items, displayConverter);

		if (index is null)
			return null;

		return items[index.Value];
	}

	/// <summary>
	/// Shows the menu as the whole screen: banner above, items in the middle and
	/// the session footer pinned below. The frame is replaced on every selection
	/// change and always released, including on cancellation.
	/// </summary>
	public MenuItem<TId>? ShowFramed<TId>(
		string title,
		IReadOnlyList<MenuItem<TId>> items,
		Func<string, string>? displayConverter = null,
		IReadOnlyList<PanelModel>? notices = null)
	{
		var selectable = items.Select(item => item.IsSelectable).ToList();
		var index = selectable.FindIndex(isSelectable => isSelectable);

		if (index < 0)
			return null;

		var labels = items
			.Select(item => displayConverter?.Invoke(item.Label) ?? item.Label)
			.ToList();

		_host.Begin(Frame(title, labels, selectable, index, notices));

		try
		{
			while (true)
			{
				var key = _input.ReadKey();
				var previous = index;

				switch (key.Key)
				{
					case ConsoleKey.Escape:
						return null;
					case ConsoleKey.Enter:
						if (selectable[index])
							return items[index];

						continue;
					case ConsoleKey.UpArrow:
						index = StepSelection(index, -1, selectable);
						break;
					case ConsoleKey.DownArrow:
						index = StepSelection(index, 1, selectable);
						break;
					default:
						continue;
				}

				if (index != previous)
					_host.Update(Frame(title, labels, selectable, index, notices));
			}
		}
		finally
		{
			_host.End();
		}
	}

	private ScreenModel Frame(
		string title,
		IReadOnlyList<string> labels,
		IReadOnlyList<bool> selectable,
		int selected,
		IReadOnlyList<PanelModel>? notices)
	{
		List<PanelModel> body = [.. notices ?? []];

		if (!string.IsNullOrEmpty(title))
			body.Add(new PanelModel([new PanelLine([new StyledText(title, TextRole.Primary)])]));

		List<PanelLine> rows = [];

		for (var i = 0; i < labels.Count; i++)
		{
			var role = i == selected ? TextRole.Accent : TextRole.Primary;
			var marker = i == selected ? _terminal.SelectionPointer : _terminal.Indent;

			rows.Add(new PanelLine([new StyledText(marker, role), new StyledText(labels[i], role)]));
		}

		body.Add(new PanelModel(rows));

		return new ScreenModel
		{
			Header = _header.BuildModel(),
			Body = body,
			Footer = _statusBar.BuildModel()
		};
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
					index = StepSelection(index, -1, selectable);
					break;
				case ConsoleKey.DownArrow:
					index = StepSelection(index, 1, selectable);
					break;
				default:
					continue;
			}

			DrawItem(firstItemTop + oldIndex, plain[oldIndex], false);
			DrawItem(firstItemTop + index, plain[index], true);
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

	private static int StepSelection(int from, int direction, IReadOnlyList<bool> selectable)
	{
		var next = from;

		for (var n = 0; n < selectable.Count; n++)
		{
			next = (next + direction + selectable.Count) % selectable.Count;

			if (selectable[next])
				return next;
		}

		return from;
	}
}
