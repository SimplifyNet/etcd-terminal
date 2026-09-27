using EtcdTerminal.Terminal;
using EtcdTerminal.App.Components;
using EtcdTerminal.Localization;
using EtcdTerminal.Theming;
using EtcdTerminal.Keys;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyBrowseLayout(ITerminalOutput _output, ITerminalCursor _cursor, ITerminalStyle _style, ILocalization _localization)
{
	private const int LinePadding = 2;
	private const int PrefixWidth = 4;

	public string SelectionColor => _style.Accent;

	private int KeyColumnWidth => (_output.WindowWidth - LinePadding - PrefixWidth - 1) / 2;

	private int ValueColumnWidth => _output.WindowWidth - LinePadding - PrefixWidth - 1 - KeyColumnWidth;

	public (int SearchEndCol, int SearchBarRow) RenderSearchBar(string searchQuery)
	{
		_output.WriteFillRow(_style.PanelBackground);

		_output.Write(_style.PanelBackground);

		if (searchQuery.Length == 0)
			_output.Write(ValuePreview.Preview(_localization.TypeToSearch, _output.WindowWidth), TerminalColor.Muted);
		else
			_output.Write($"  \U0001f50d {_style.Primary}{BoundQuery(searchQuery)}{_style.Reset}");

		var searchEndCol = _cursor.CursorLeft;
		var searchBarRow = _cursor.CursorTop;

		_output.PadCurrentRow(_style.PanelBackground);
		_output.WriteLine();

		_output.Write(_output.FillRow(_style.PanelBackground));

		return (searchEndCol, searchBarRow);
	}

	private string BoundQuery(string searchQuery) =>
		ValuePreview.Preview(searchQuery, Math.Max(0, _output.WindowWidth - 8));

	private string BoundStyled(string content, int maxCells) =>
		_output.GetVisibleLength(content) <= maxCells ? content : DisplayCells.TruncateStyled(content, maxCells) + _style.Reset;

	public void RenderKeyList(IReadOnlyList<EtcdKeyValue> pageKeys, int selectedIndex)
	{
		if (pageKeys.Count == 0)
		{
			_output.WriteIndentedLine(_localization.NoKeysFound, TerminalColor.Muted);

			return;
		}

		var keyWidth = Math.Max(0, KeyColumnWidth);
		var valueWidth = Math.Max(0, ValueColumnWidth);

		for (var i = 0; i < pageKeys.Count; i++)
		{
			var kv = pageKeys[i];
			var isSelected = i == selectedIndex;

			var prefix = isSelected ? _style.SelectionPointer : _style.Indent;
			var key = ValuePreview.Preview(kv.Key, keyWidth);
			var value = ValuePreview.Preview(kv.Value, valueWidth);
			var paddedKey = key + new string(' ', Math.Max(0, keyWidth - DisplayCells.Width(key)));
			var line = ValuePreview.Preview($"  {prefix}{paddedKey} {value}", _output.WindowWidth);

			if (isSelected)
				_output.Write($"{_style.Accent}{line}{_style.Reset}\n");
			else
				_output.Write($"{_style.Primary}{line}{_style.Reset}\n");
		}
	}

	public void RenderPagination(int currentPage, int totalPages, int totalKeys)
	{
		var currentPageLabel = currentPage + 1;
		var bg = _style.PanelBackground;
		var content = $"{bg}{_style.Muted}  {_localization.Page} {_style.Primary}{currentPageLabel}/{totalPages}{_style.Muted}  \u2022  {_style.Primary}{totalKeys}{_style.Muted} {_localization.TotalKeys}{_style.Reset}";

		_output.WriteFillRow(bg);
		_output.Write(BoundStyled(content, _output.WindowWidth));
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
		var labelWidth = DisplayCells.Width("  " + _localization.Selected + " ");
		var key = ValuePreview.Preview(selectedKey, Math.Max(0, _output.WindowWidth - 1 - labelWidth));

		_output.WriteBorderedFillRow(_style.PanelDarkerBackground);
		_output.WriteBorderedRow(_style.PanelDarkerBackground, $"{_style.Muted}  {_localization.Selected} {_style.Accent}{key}");
		_output.WriteBorderedFillRow(_style.PanelDarkerBackground);
	}

	private void RenderButtonsPanel(bool canModify)
	{
		List<(string Key, string Label)> buttons = [];

		if (canModify)
		{
			buttons.Add(("E", _localization.Edit));
			buttons.Add(("D", _localization.Delete));
		}

		buttons.Add(("Esc", _localization.Cancel));

		var colored = "  " + string.Join("   ", buttons.Select(b => $"{_style.Primary}{b.Key} {_style.Muted}{b.Label}"));

		_output.WriteBorderedFillRow(_style.PanelBackground);
		_output.WriteBorderedRow(_style.PanelBackground, BoundStyled(colored, _output.WindowWidth - 1));
		_output.WriteBorderedFillRow(_style.PanelBackground);
	}
}
