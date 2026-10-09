using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Engine;

/// <summary>
/// Selection over a list of items with stable identifiers. The menu opens the
/// screen first and lets the selection prompt own everything below the banner.
/// </summary>
public sealed class Menu(Screen _screen, ISelectionPrompt _selection)
{
	public Choice<TId>? Show<TId>(string? title, IReadOnlyList<Choice<TId>> items, IReadOnlyList<Block>? preamble = null, Action<Choice<TId>>? onHighlight = null, TId? selectedId = default)
	{
		if (items.Count is 0)
			return null;

		_screen.Open(preamble);

		return _selection.Select(new ChoiceList<TId>(title, items, selectedId), onHighlight);
	}

	/// <summary>
	/// A selection that is one step of a dialog: it opens below what the
	/// screen already shows instead of a new screen, and the answer stays on
	/// screen as a line, the way a typed answer does.
	/// </summary>
	public Choice<TId>? Ask<TId>(string title, IReadOnlyList<Choice<TId>> items)
	{
		if (items.Count is 0)
			return null;

		var choice = _selection.Select(new ChoiceList<TId>(title, items));

		if (choice is not null)
			_screen.Write(TextBlock.Line(new StyledText(title + " "), new StyledText(choice.Label, TextRole.Accent)));

		return choice;
	}
}
