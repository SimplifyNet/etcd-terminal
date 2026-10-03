using EtcdTerminal.Presentation.Terminal;
using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Engine;

/// <summary>
/// Selection over a list of items with stable identifiers. The menu is always
/// the only writer of its screen: it owns a frame that is replaced in place and
/// released on completion, including on cancellation.
/// </summary>
public sealed class Menu(ITerminal _terminal, ITerminalInput _input, IScreenHost _host, Header _header, StatusBar _statusBar)
{
	/// <summary>
	/// Shows the menu as the whole screen: banner above, items in the middle and
	/// the session footer pinned below. The frame is replaced on every selection
	/// change and always released, including on cancellation.
	/// </summary>
	public MenuItem<TId>? ShowFramed<TId>(
		string title,
		IReadOnlyList<MenuItem<TId>> items,
		Func<string, string>? displayConverter = null,
		IReadOnlyList<Block>? notices = null)
	{
		var selectable = items.Select(item => item.IsSelectable).ToList();
		var index = selectable.FindIndex(isSelectable => isSelectable);

		if (index < 0)
			return null;

		var labels = items
			.Select(item => displayConverter?.Invoke(item.Label) ?? item.Label)
			.ToList();

		_host.Begin(Frame(title, labels, index, notices));

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
					_host.Update(Frame(title, labels, index, notices));
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
		int selected,
		IReadOnlyList<Block>? notices)
	{
		List<Block> body = [.. notices ?? []];

		if (!string.IsNullOrEmpty(title))
			body.Add(new TitleBlock(new StyledText(_terminal.Indent + title, TextRole.Primary)));

		List<IReadOnlyList<StyledText>> rows = [];

		for (var i = 0; i < labels.Count; i++)
		{
			var role = i == selected ? TextRole.Accent : TextRole.Primary;
			var marker = i == selected ? _terminal.SelectionPointer : _terminal.Indent;

			rows.Add([new StyledText(marker, role), new StyledText(labels[i], role)]);
		}

		body.Add(new TextBlock(rows));

		return new ScreenModel
		{
			Header = _header.BuildModel(),
			Body = body,
			Footer = _statusBar.BuildModel()
		};
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
