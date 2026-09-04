using EtcdTerminal.Terminal;
using EtcdTerminal.App.Components;
using EtcdTerminal.Localization;
using EtcdTerminal.Theming;
using Spectre.Console;
using EtcdTerminal.Keys;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyBrowseLayout(ITerminal _terminal)
{
	private const int LinePadding = 2;
	private const int PrefixWidth = 4;

	public string SelectionColor => $"[#{ThemeStore.Current.Accent.R:X2}{ThemeStore.Current.Accent.G:X2}{ThemeStore.Current.Accent.B:X2}]";

	private int KeyColumnWidth => (_terminal.WindowWidth - LinePadding - PrefixWidth - 1) / 2;

	private int ValueColumnWidth => _terminal.WindowWidth - LinePadding - PrefixWidth - 1 - KeyColumnWidth;

	public static string TruncateText(string text, int maxLength) =>
		text.Length <= maxLength ? text : text[..maxLength] + "...";

	public (int SearchEndCol, int SearchBarRow) RenderSearchBar(string searchQuery)
	{
		_terminal.WriteFillRow(_terminal.Bg);

		_terminal.Write(_terminal.Bg);

		if (searchQuery.Length == 0)
			AnsiConsole.Markup($"[grey]{LocalizationStore.Current.TypeToSearch}[/]");
		else
			AnsiConsole.Markup($"  \U0001f50d [white]{Markup.Escape(searchQuery)}[/]");

		var searchEndCol = _terminal.CursorLeft;
		var searchBarRow = _terminal.CursorTop;

		_terminal.PadCurrentRow(_terminal.Bg);
		_terminal.WriteLine();

		_terminal.Write(_terminal.FillRow(_terminal.Bg));

		return (searchEndCol, searchBarRow);
	}

	public void RenderKeyList(IReadOnlyList<EtcdKeyValue> pageKeys, int selectedIndex)
	{
		if (pageKeys.Count == 0)
		{
			AnsiConsole.MarkupLine($"[grey]{LocalizationStore.Current.NoKeysFound}[/]");

			return;
		}

		var keyWidth = KeyColumnWidth;
		var valueWidth = ValueColumnWidth;

		for (var i = 0; i < pageKeys.Count; i++)
		{
			var kv = pageKeys[i];
			var isSelected = i == selectedIndex;

			var prefix = isSelected ? _terminal.SelectionPointer : _terminal.SelectionPointerEmpty;
			var key = TruncateText(kv.Key, keyWidth);
			var value = TruncateText(kv.Value, valueWidth);
			var line = $"{prefix}{key.PadRight(keyWidth)} {value}";

			if (isSelected)
				AnsiConsole.MarkupLine($"  {SelectionColor}{Markup.Escape(line)}[/]");
			else
				AnsiConsole.MarkupLine($"  [white]{Markup.Escape(line)}[/]");
		}
	}

	public void RenderPagination(int currentPage, int totalPages, int totalKeys)
	{
		var currentPageLabel = currentPage + 1;

		_terminal.WriteFillRow(_terminal.Bg);
		_terminal.Write($"{_terminal.Bg}{_terminal.Grey}  {LocalizationStore.Current.Page} {_terminal.White}{currentPageLabel}/{totalPages}{_terminal.Grey}  •  {_terminal.White}{totalKeys}{_terminal.Grey} {LocalizationStore.Current.TotalKeys}{_terminal.Reset}");
		_terminal.PadCurrentRow(_terminal.Bg);
		_terminal.WriteLine();
		_terminal.WriteFillRow(_terminal.Bg);
	}

	public void RenderActionBar(string selectedKey)
	{
		RenderSelectedPanel(selectedKey);
		RenderButtonsPanel();
	}

	private void RenderSelectedPanel(string selectedKey)
	{
		_terminal.WriteBorderedFillRow(_terminal.DarkBg);
		_terminal.WriteBorderedRow(_terminal.DarkBg, $"{_terminal.Grey}  {LocalizationStore.Current.Selected} {_terminal.Accent}{selectedKey}");
		_terminal.WriteBorderedFillRow(_terminal.DarkBg);
	}

	private void RenderButtonsPanel()
	{
		(string Key, string Label)[] buttons = [("E", LocalizationStore.Current.Edit), ("D", LocalizationStore.Current.Delete), ("Esc", LocalizationStore.Current.Cancel)];
		var colored = "  " + string.Join("   ", buttons.Select(b => $"{_terminal.White}{b.Key} {_terminal.Grey}{b.Label}"));

		_terminal.WriteBorderedFillRow(_terminal.Bg);
		_terminal.WriteBorderedRow(_terminal.Bg, colored);
		_terminal.WriteBorderedFillRow(_terminal.Bg);
	}
}
