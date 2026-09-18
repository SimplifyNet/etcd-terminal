using EtcdTerminal.Terminal;
using EtcdTerminal.App.Components;
using EtcdTerminal.Localization;
using EtcdTerminal.Theming;
using EtcdTerminal.Keys;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyBrowseLayout(ITerminalOutput _output, ITerminalCursor _cursor, ITerminalStyle _style)
{
	private const int LinePadding = 2;
	private const int PrefixWidth = 4;

	public string SelectionColor => _style.Accent;

	private int KeyColumnWidth => (_output.WindowWidth - LinePadding - PrefixWidth - 1) / 2;

	private int ValueColumnWidth => _output.WindowWidth - LinePadding - PrefixWidth - 1 - KeyColumnWidth;

	public static string TruncateText(string text, int maxLength) =>
		text.Length <= maxLength ? text : text[..maxLength] + "...";

	public (int SearchEndCol, int SearchBarRow) RenderSearchBar(string searchQuery)
	{
		_output.WriteFillRow(_style.Bg);

		_output.Write(_style.Bg);

		if (searchQuery.Length == 0)
			_output.Write(LocalizationStore.Current.TypeToSearch, TerminalColor.Muted);
		else
			_output.Write($"  \U0001f50d {_style.White}{searchQuery}{_style.Reset}");

		var searchEndCol = _cursor.CursorLeft;
		var searchBarRow = _cursor.CursorTop;

		_output.PadCurrentRow(_style.Bg);
		_output.WriteLine();

		_output.Write(_output.FillRow(_style.Bg));

		return (searchEndCol, searchBarRow);
	}

	public void RenderKeyList(IReadOnlyList<EtcdKeyValue> pageKeys, int selectedIndex)
	{
		if (pageKeys.Count == 0)
		{
			_output.WriteIndentedLine(LocalizationStore.Current.NoKeysFound, TerminalColor.Muted);

			return;
		}

		var keyWidth = KeyColumnWidth;
		var valueWidth = ValueColumnWidth;

		for (var i = 0; i < pageKeys.Count; i++)
		{
			var kv = pageKeys[i];
			var isSelected = i == selectedIndex;

			var prefix = isSelected ? _style.SelectionPointer : _style.Indent;
			var key = TruncateText(kv.Key, keyWidth);
			var value = TruncateText(kv.Value, valueWidth);
			var line = $"{prefix}{key.PadRight(keyWidth)} {value}";

			if (isSelected)
				_output.Write($"  {_style.Accent}{line}{_style.Reset}\n");
			else
				_output.Write($"  {_style.White}{line}{_style.Reset}\n");
		}
	}

	public void RenderPagination(int currentPage, int totalPages, int totalKeys)
	{
		var currentPageLabel = currentPage + 1;
		var bg = _style.Bg;
		var content = $"{bg}{_style.Grey}  {LocalizationStore.Current.Page} {_style.White}{currentPageLabel}/{totalPages}{_style.Grey}  \u2022  {_style.White}{totalKeys}{_style.Grey} {LocalizationStore.Current.TotalKeys}{_style.Reset}";

		_output.WriteFillRow(bg);
		_output.Write(content);
		_output.PadCurrentRow(bg);
		_output.WriteLine();
		_output.WriteFillRow(bg);
	}

	public void RenderActionBar(string selectedKey, bool canModify)
	{
		RenderSelectedPanel(selectedKey);
		RenderButtonsPanel(canModify);
	}

	private void RenderSelectedPanel(string selectedKey)
	{
		_output.WriteBorderedFillRow(_style.DarkBg);
		_output.WriteBorderedRow(_style.DarkBg, $"{_style.Grey}  {LocalizationStore.Current.Selected} {_style.Accent}{selectedKey}");
		_output.WriteBorderedFillRow(_style.DarkBg);
	}

	private void RenderButtonsPanel(bool canModify)
	{
		List<(string Key, string Label)> buttons = [];

		if (canModify)
		{
			buttons.Add(("E", LocalizationStore.Current.Edit));
			buttons.Add(("D", LocalizationStore.Current.Delete));
		}

		buttons.Add(("Esc", LocalizationStore.Current.Cancel));

		var colored = "  " + string.Join("   ", buttons.Select(b => $"{_style.White}{b.Key} {_style.Grey}{b.Label}"));

		_output.WriteBorderedFillRow(_style.Bg);
		_output.WriteBorderedRow(_style.Bg, colored);
		_output.WriteBorderedFillRow(_style.Bg);
	}
}
