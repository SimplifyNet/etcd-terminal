using System.Globalization;
using EtcdTerminal.Terminal;
using EtcdTerminal.App.Components;
using EtcdTerminal.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Theming;
using EtcdTerminal.Keys;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyBrowseLayout(ITerminalOutput _output, ITerminalCursor _cursor, ITerminalStyle _style, ILocalization _localization, IPanelRenderer _panels)
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

	public void RenderPagination(int currentPage, int totalPages, int totalKeys) =>
		_panels.Write(new PanelModel(
		[
			new PanelLine(
			[
				new StyledText($"{_localization.Page} ", TextRole.Muted),
				new StyledText($"{currentPage + 1}/{totalPages}", TextRole.Primary),
				new StyledText("  \u2022  ", TextRole.Muted),
				new StyledText(totalKeys.ToString(CultureInfo.InvariantCulture), TextRole.Primary),
				new StyledText($" {_localization.TotalKeys}", TextRole.Muted)
			])
		]));

	public void RenderActionBar(string selectedKey, bool canModify)
	{
		RenderSelectedPanel(selectedKey);
		RenderButtonsPanel(canModify);
	}

	private void RenderSelectedPanel(string selectedKey) =>
		_panels.Write(new PanelModel(
		[
			new PanelLine(
			[
				new StyledText($"{_localization.Selected} ", TextRole.Muted),
				new StyledText(ValuePreview.Sanitize(selectedKey), TextRole.Accent)
			])
		], PanelKind.Selection));

	private void RenderButtonsPanel(bool canModify)
	{
		List<StyledText> hints = [];

		if (canModify)
		{
			hints.Add(new StyledText("E", TextRole.Primary));
			hints.Add(new StyledText($" {_localization.Edit}", TextRole.Muted));
			hints.Add(new StyledText("   ", TextRole.Muted));
			hints.Add(new StyledText("D", TextRole.Primary));
			hints.Add(new StyledText($" {_localization.Delete}", TextRole.Muted));
			hints.Add(new StyledText("   ", TextRole.Muted));
		}

		hints.Add(new StyledText("Esc", TextRole.Primary));
		hints.Add(new StyledText($" {_localization.Cancel}", TextRole.Muted));

		_panels.Write(new PanelModel([new PanelLine(hints)], PanelKind.Actions));
	}
}
