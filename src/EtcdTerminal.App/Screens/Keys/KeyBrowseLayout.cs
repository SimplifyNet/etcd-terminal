using EtcdTerminal.Terminal;
using EtcdTerminal.App.Components;
using EtcdTerminal.Localization;
using EtcdTerminal.Theming;
using EtcdTerminal.Keys;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyBrowseLayout(ITerminal _terminal)
{
	private const int LinePadding = 2;
	private const int PrefixWidth = 4;

	public string SelectionColor => _terminal.Accent;

	private int KeyColumnWidth => (_terminal.WindowWidth - LinePadding - PrefixWidth - 1) / 2;

	private int ValueColumnWidth => _terminal.WindowWidth - LinePadding - PrefixWidth - 1 - KeyColumnWidth;

	public static string TruncateText(string text, int maxLength) =>
		text.Length <= maxLength ? text : text[..maxLength] + "...";

	public (int SearchEndCol, int SearchBarRow) RenderSearchBar(string searchQuery)
	{
		_terminal.WriteFillRow(_terminal.Bg);

		_terminal.Write(_terminal.Bg);

		if (searchQuery.Length == 0)
			_terminal.Write(LocalizationStore.Current.TypeToSearch, TerminalColor.Muted);
		else
		{
			_terminal.Write($"  \U0001f50d {_terminal.White}{searchQuery}{_terminal.Reset}");
		}

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
			_terminal.WriteIndentedLine(LocalizationStore.Current.NoKeysFound, TerminalColor.Muted);

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
				_terminal.Write($"  {_terminal.Accent}{line}{_terminal.Reset}\n");
			else
				_terminal.Write($"  {_terminal.White}{line}{_terminal.Reset}\n");
		}
	}

	public void RenderPagination(int currentPage, int totalPages, int totalKeys)
	{
		var currentPageLabel = currentPage + 1;
		var bg = _terminal.Bg;
		var content = $"{bg}{_terminal.Grey}  {LocalizationStore.Current.Page} {_terminal.White}{currentPageLabel}/{totalPages}{_terminal.Grey}  \u2022  {_terminal.White}{totalKeys}{_terminal.Grey} {LocalizationStore.Current.TotalKeys}{_terminal.Reset}";

		_terminal.WriteFillRow(bg);
		_terminal.Write(content);
		_terminal.PadCurrentRow(bg);
		_terminal.WriteLine();
		_terminal.WriteFillRow(bg);
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
