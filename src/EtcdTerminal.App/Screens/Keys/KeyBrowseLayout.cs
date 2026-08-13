using EtcdTerminal.App.Components;
using Spectre.Console;
using EtcdTerminal.Keys;

namespace EtcdTerminal.App.Screens.Keys;

public static class KeyBrowseLayout
{
	private const int LinePadding = 2;
	private const int PrefixWidth = 4;

	private const string NoKeysFound = "  [grey]No keys found.[/]";

	public static string SelectionColor => $"[#{TerminalPanel.AccentColor.ToHex()}]";

	private static int KeyColumnWidth => (Console.WindowWidth - LinePadding - PrefixWidth - 1) / 2;

	private static int ValueColumnWidth => Console.WindowWidth - LinePadding - PrefixWidth - 1 - KeyColumnWidth;

	public static (int SearchEndCol, int SearchBarRow) RenderSearchBar(string searchQuery)
	{
		TerminalPanel.WriteFillRow(TerminalPanel.Bg);

		Console.Write(TerminalPanel.Bg);

		if (searchQuery.Length == 0)
			AnsiConsole.Markup("[grey]  \U0001f50d  Type to search...[/]");
		else
			AnsiConsole.Markup($"  \U0001f50d [white]{Markup.Escape(searchQuery)}[/]");

		var searchEndCol = Console.CursorLeft;
		var searchBarRow = Console.CursorTop;

		TerminalPanel.PadCurrentRow(TerminalPanel.Bg);
		Console.WriteLine();

		Console.Write(TerminalPanel.FillRow(TerminalPanel.Bg));

		return (searchEndCol, searchBarRow);
	}

	public static void RenderKeyList(IReadOnlyList<EtcdKeyValue> pageKeys, int selectedIndex)
	{
		if (pageKeys.Count == 0)
		{
			AnsiConsole.MarkupLine(NoKeysFound);

			return;
		}

		var keyWidth = KeyColumnWidth;
		var valueWidth = ValueColumnWidth;

		for (var i = 0; i < pageKeys.Count; i++)
		{
			var kv = pageKeys[i];
			var isSelected = i == selectedIndex;

			var prefix = isSelected ? TerminalPanel.SelectionPointer : TerminalPanel.SelectionPointerEmpty;
			var key = TruncateText(kv.Key, keyWidth);
			var value = TruncateText(kv.Value, valueWidth);
			var line = $"{prefix}{key.PadRight(keyWidth)} {value}";

			if (isSelected)
				AnsiConsole.MarkupLine($"  {SelectionColor}{Markup.Escape(line)}[/]");
			else
				AnsiConsole.MarkupLine($"  [white]{Markup.Escape(line)}[/]");
		}
	}

	public static void RenderPagination(int currentPage, int totalPages, int totalKeys)
	{
		var currentPageLabel = currentPage + 1;

		TerminalPanel.WriteFillRow(TerminalPanel.Bg);
		Console.Write($"{TerminalPanel.Bg}{TerminalPanel.Grey}  Page {TerminalPanel.White}{currentPageLabel}/{totalPages}{TerminalPanel.Grey}  •  {TerminalPanel.White}{totalKeys}{TerminalPanel.Grey} total keys{TerminalPanel.Reset}");
		TerminalPanel.PadCurrentRow(TerminalPanel.Bg);
		Console.WriteLine();
		TerminalPanel.WriteFillRow(TerminalPanel.Bg);
	}

	public static void RenderActionBar(string selectedKey)
	{
		RenderSelectedPanel(selectedKey);
		RenderButtonsPanel();
	}

	private static void RenderSelectedPanel(string selectedKey)
	{
		TerminalPanel.WriteBorderedFillRow(TerminalPanel.DarkBg);
		TerminalPanel.WriteBorderedRow(TerminalPanel.DarkBg, $"{TerminalPanel.Grey}  Selected: {TerminalPanel.Accent}{selectedKey}");
		TerminalPanel.WriteBorderedFillRow(TerminalPanel.DarkBg);
	}

	private static void RenderButtonsPanel()
	{
		(string Key, string Label)[] buttons = [("E", "Edit"), ("D", "Delete"), ("Esc", "Cancel")];
		var colored = "  " + string.Join("   ", buttons.Select(b => $"{TerminalPanel.White}{b.Key} {TerminalPanel.Grey}{b.Label}"));

		TerminalPanel.WriteBorderedFillRow(TerminalPanel.Bg);
		TerminalPanel.WriteBorderedRow(TerminalPanel.Bg, colored);
		TerminalPanel.WriteBorderedFillRow(TerminalPanel.Bg);
	}

	public static string TruncateText(string text, int maxLength) =>
		text.Length <= maxLength ? text : text[..maxLength] + "...";
}
