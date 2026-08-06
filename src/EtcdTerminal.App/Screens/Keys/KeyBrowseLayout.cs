using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Screens.Keys;

public static class KeyBrowseLayout
{
	private const int LinePadding = 2;
	private const int PrefixWidth = 4;

	private const string PanelBg = "\x1b[48;2;27;28;30m";
	private const string SelectedPanelBg = "\x1b[48;2;21;22;24m";
	private const string WhiteFg = "\x1b[38;2;255;255;255m";
	private const string GreyFg = "\x1b[38;2;128;128;128m";
	private const string AccentFg = "\x1b[38;2;220;95;51m";
	private const string Reset = "\x1b[0m";

	private const string NoKeysFound = "  [grey]No keys found.[/]";

	public const string SelectionColor = "[#dc5f33]";

	private static int KeyColumnWidth => (Console.WindowWidth - LinePadding - PrefixWidth - 1) / 2;

	private static int ValueColumnWidth => Console.WindowWidth - LinePadding - PrefixWidth - 1 - KeyColumnWidth;

	public static (int SearchEndCol, int SearchBarRow) RenderSearchBar(string searchQuery)
	{
		var fill = new string(' ', Console.WindowWidth);

		Console.Write(PanelBg + fill + Reset);
		Console.WriteLine();

		Console.Write(PanelBg);

		if (searchQuery.Length == 0)
			AnsiConsole.Markup("[grey]  \U0001f50d  Type to search...[/]");
		else
			AnsiConsole.Markup($"  \U0001f50d [white]{Markup.Escape(searchQuery)}[/]");

		var searchEndCol = Console.CursorLeft;
		var searchBarRow = Console.CursorTop;

		var remaining = Console.WindowWidth - searchEndCol;

		if (remaining > 0)
			Console.Write(PanelBg + new string(' ', remaining) + Reset);

		Console.WriteLine();

		Console.Write(PanelBg + fill + Reset);

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

			var prefix = isSelected ? "  ❯ " : "    ";
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
		var fill = new string(' ', Console.WindowWidth);
		var currentPageLabel = currentPage + 1;

		Console.Write(PanelBg + fill + Reset);
		Console.WriteLine();

		Console.Write(PanelBg + GreyFg + "  Page " + WhiteFg + currentPageLabel + "/" + totalPages + GreyFg + "  •  " + WhiteFg + totalKeys + GreyFg + " total keys" + Reset);
		var remaining = Console.WindowWidth - Console.CursorLeft;

		if (remaining > 0)
			Console.Write(PanelBg + new string(' ', remaining) + Reset);

		Console.WriteLine();
		Console.WriteLine(PanelBg + fill + Reset);
	}

	public static void RenderActionBar(string selectedKey)
	{
		RenderSelectedPanel(selectedKey);

		Console.WriteLine();

		RenderButtonsPanel();
	}

	private static void RenderSelectedPanel(string selectedKey)
	{
		var fill = new string(' ', Console.WindowWidth);
		var visible = $"  Selected: {selectedKey}";

		Console.WriteLine($"{SelectedPanelBg}{fill}{Reset}");
		Console.WriteLine($"{SelectedPanelBg}{GreyFg}  Selected: {AccentFg}{selectedKey}{new string(' ', Math.Max(0, Console.WindowWidth - visible.Length))}{Reset}");
		Console.WriteLine($"{SelectedPanelBg}{fill}{Reset}");
	}

	private static void RenderButtonsPanel()
	{
		var fill = new string(' ', Console.WindowWidth);
		var buttons = new (string Key, string Label)[] { ("E", "Edit"), ("D", "Delete"), ("Esc", "Cancel") };
		var visible = "  " + string.Join("   ", buttons.Select(b => $"{b.Key} {b.Label}"));
		var colored = "  " + string.Join("   ", buttons.Select(b => $"{WhiteFg}{b.Key} {GreyFg}{b.Label}"));

		Console.WriteLine($"{PanelBg}{fill}{Reset}");
		Console.WriteLine($"{PanelBg}{colored}{new string(' ', Math.Max(0, Console.WindowWidth - visible.Length))}{Reset}");
		Console.WriteLine($"{PanelBg}{fill}{Reset}");
	}

	public static string TruncateText(string text, int maxLength) =>
		text.Length <= maxLength ? text : text[..maxLength] + "...";
}
