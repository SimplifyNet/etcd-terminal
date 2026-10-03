using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Engine;

/// <summary>
/// Selection over a list of items with stable identifiers. The menu opens the
/// screen first and lets the selection prompt own everything below the banner.
/// </summary>
public sealed class Menu(Screen _screen, ISelectionPrompt _selection)
{
	public Choice<TId>? Show<TId>(string? title, IReadOnlyList<Choice<TId>> items, IReadOnlyList<Block>? preamble = null)
	{
		if (items.Count is 0)
			return null;

		_screen.Open(preamble);

		return _selection.Select(new ChoiceList<TId>(title, items));
	}
}
