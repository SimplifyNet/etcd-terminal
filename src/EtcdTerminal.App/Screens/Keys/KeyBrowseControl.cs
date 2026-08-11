using EtcdTerminal.App.Components;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Screens.Keys;

public enum KeyBrowseAction
{
	None,
	SearchChanged,
	Edit,
	Delete,
	Exit,
}

public readonly record struct KeyBrowseCommand(KeyBrowseAction Action, EtcdKeyValue? SelectedKey)
{
	public static KeyBrowseCommand None => new(KeyBrowseAction.None, null);
}

public sealed class KeyBrowseControl(EtcdConnectionConfig _config)
{
	public string SearchQuery { get; private set; } = "";
	public int CurrentPage { get; private set; }
	public int SelectedIndex { get; private set; }
	public bool ShowActions { get; private set; }
	public EtcdKeyValue? SelectedKey { get; private set; }

	public void Render(IReadOnlyList<EtcdKeyValue> pageKeys, int totalPages, int totalKeys)
	{
		AnsiConsole.Clear();
		Header.Render();

		var (searchEndCol, searchBarRow) = KeyBrowseLayout.RenderSearchBar(SearchQuery);

		Console.SetCursorPosition(0, Console.CursorTop + 2);
		KeyBrowseLayout.RenderKeyList(pageKeys, SelectedIndex);

		Console.WriteLine();
		KeyBrowseLayout.RenderPagination(CurrentPage, totalPages, totalKeys);

		Console.WriteLine();
		if (ShowActions && SelectedKey is not null)
			KeyBrowseLayout.RenderActionBar(SelectedKey.Key);

		StatusBar.Render(_config);

		Console.CursorTop = searchBarRow;
		Console.CursorLeft = searchEndCol;
	}

	public KeyBrowseCommand ReadCommand(IReadOnlyList<EtcdKeyValue> pageKeys, int totalPages)
	{
		var key = Console.ReadKey(true);

		if (ShowActions)
		{
			switch (key.Key)
			{
				case ConsoleKey.Escape:
					ShowActions = false;
					SelectedKey = null;
					break;
				case ConsoleKey.E:
					var editKey = SelectedKey;
					ShowActions = false;
					SelectedKey = null;
					return new KeyBrowseCommand(KeyBrowseAction.Edit, editKey);
				case ConsoleKey.D:
					var deleteKey = SelectedKey;
					ShowActions = false;
					SelectedKey = null;
					return new KeyBrowseCommand(KeyBrowseAction.Delete, deleteKey);
			}

			return KeyBrowseCommand.None;
		}

		switch (key.Key)
		{
			case ConsoleKey.UpArrow:
				if (SelectedIndex > 0)
					SelectedIndex--;
				break;
			case ConsoleKey.DownArrow:
				if (SelectedIndex < pageKeys.Count - 1)
					SelectedIndex++;
				break;
			case ConsoleKey.LeftArrow:
				if (CurrentPage > 0)
				{
					CurrentPage--;
					SelectedIndex = 0;
				}
				break;
			case ConsoleKey.RightArrow:
				if (CurrentPage < totalPages - 1)
				{
					CurrentPage++;
					SelectedIndex = 0;
				}
				break;
			case ConsoleKey.Enter:
				if (pageKeys.Count > 0)
				{
					SelectedKey = pageKeys[Math.Min(SelectedIndex, pageKeys.Count - 1)];
					ShowActions = true;
				}
				break;
			case ConsoleKey.Backspace:
				if (SearchQuery.Length > 0)
				{
					SearchQuery = SearchQuery[..^1];
					return new KeyBrowseCommand(KeyBrowseAction.SearchChanged, null);
				}
				break;
			case ConsoleKey.Escape:
				return new KeyBrowseCommand(KeyBrowseAction.Exit, null);
			default:
				if (!char.IsControl(key.KeyChar))
				{
					SearchQuery += key.KeyChar;
					return new KeyBrowseCommand(KeyBrowseAction.SearchChanged, null);
				}
				break;
		}

		return KeyBrowseCommand.None;
	}

	public void ResetNavigation()
	{
		CurrentPage = 0;
		SelectedIndex = 0;
	}

	public void ClampPage(int totalPages)
	{
		CurrentPage = Math.Clamp(CurrentPage, 0, Math.Max(0, totalPages - 1));
		SelectedIndex = 0;
	}
}
