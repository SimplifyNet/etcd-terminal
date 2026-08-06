using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Screens;

public sealed class KeyBrowseScreen(IEtcdClient _etcdClient)
{
	private const int PageSize = 30;
	private const int LinePadding = 2;
	private const int PrefixWidth = 4;
	private const int EditValueMaxLength = 200;

	private const string NoKeysFound = "  [grey]No keys found.[/]";
	private const string SelectionColor = "[#dc5f33]";
	private const string KeyUpdated = "[green]Key updated successfully![/]";
	private const string CouldNotUpdateKey = "[red]Could not update key.[/]";
	private const string KeyDeleted = "[green]Key deleted successfully![/]";
	private const string KeyCouldNotBeDeleted = "[red]Key could not be deleted.[/]";
	private const string EnterNewValue = "Enter new value:";
	private const string AreYouSure = "Are you sure?";
	private const string PanelBg = "\x1b[48;2;27;28;30m";
	private const string SelectedPanelBg = "\x1b[48;2;21;22;24m";
	private const string WhiteFg = "\x1b[38;2;255;255;255m";
	private const string GreyFg = "\x1b[38;2;128;128;128m";
	private const string AccentFg = "\x1b[38;2;220;95;51m";
	private const string Reset = "\x1b[0m";

	private List<EtcdKeyValue> _allKeys = [];
	private List<EtcdKeyValue> _filteredKeys = [];
	private string _searchQuery = "";
	private int _currentPage;
	private int _selectedIndex;
	private bool _showActions;
	private EtcdKeyValue? _selectedKey;
	private EtcdConnectionConfig _config = default!;
	private int _searchEndCol;
	private int _searchBarRow;

	private static int KeyColumnWidth => (Console.WindowWidth - LinePadding - PrefixWidth - 1) / 2;
	private static int ValueColumnWidth => Console.WindowWidth - LinePadding - PrefixWidth - 1 - KeyColumnWidth;

	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		_config = config;
		_searchQuery = "";
		_currentPage = 0;
		_selectedIndex = 0;
		_showActions = false;
		_selectedKey = null;

		await LoadKeysAsync();

		await MainLoopAsync();
	}

	private async Task LoadKeysAsync()
	{
		var authEnabled = await _etcdClient.IsAuthenticationEnabledAsync();

		async Task<List<EtcdKeyValue>> LoadAllKeysAsync()
		{
			var keys = await _etcdClient.GetKeysByPrefixAsync("");

			if (keys.Count > 0)
				return [.. keys];

			return [.. await _etcdClient.GetKeysByPrefixAsync("/")];
		}

		if (!authEnabled)
			_allKeys = await LoadAllKeysAsync();
		else
		{
			var username = _config.Username;

			if (username is null)
			{
				_allKeys = await LoadAllKeysAsync();
				_filteredKeys = [.. _allKeys];

				return;
			}

			var user = await _etcdClient.GetUserAsync(username);

			if (user is null || user.Roles.Count == 0 || user.Roles.Contains("root"))
				_allKeys = await LoadAllKeysAsync();
			else
			{
				var keys = new List<EtcdKeyValue>();

				foreach (var roleName in user.Roles)
				{
					var role = await _etcdClient.GetRoleAsync(roleName);

					if (role is null)
						continue;

					foreach (var perm in role.Permissions)
					{
						if (perm.Type is not (PermissionType.Read or PermissionType.ReadWrite))
							continue;

						var prefix = perm.KeyPrefix;

						if (prefix == "\0")
							prefix = "";

						var prefixKeys = await _etcdClient.GetKeysByPrefixAsync(prefix);

						keys.AddRange(prefixKeys);
					}
				}

				_allKeys = [.. keys.DistinctBy(kv => kv.Key)];
			}
		}

		_filteredKeys = [.. _allKeys];
	}

	private async Task MainLoopAsync()
	{
		while (true)
		{
			Render();

			var keyInfo = Console.ReadKey(true);

			if (_showActions)
			{
				switch (keyInfo.Key)
				{
					case ConsoleKey.Escape:
						_showActions = false;
						_selectedKey = null;
						break;
					case ConsoleKey.E:
						await EditKeyAsync();
						_showActions = false;
						_selectedKey = null;
						break;
					case ConsoleKey.D:
						await DeleteKeyAsync();
						_showActions = false;
						_selectedKey = null;
						break;
				}

				continue;
			}

			switch (keyInfo.Key)
			{
				case ConsoleKey.UpArrow:
					if (_selectedIndex > 0)
						_selectedIndex--;
					break;
				case ConsoleKey.DownArrow:
					var pageItemCount = GetPageItemCount();

					if (_selectedIndex < pageItemCount - 1)
						_selectedIndex++;
					break;
				case ConsoleKey.LeftArrow:
					if (_currentPage > 0)
					{
						_currentPage--;
						_selectedIndex = 0;
					}
					break;
				case ConsoleKey.RightArrow:
					if (_currentPage < GetTotalPages() - 1)
					{
						_currentPage++;
						_selectedIndex = 0;
					}
					break;
				case ConsoleKey.Enter:
					var selected = GetCurrentPageKey();

					if (selected is not null)
					{
						_selectedKey = selected;
						_showActions = true;
					}
					break;
				case ConsoleKey.Backspace:
					if (_searchQuery.Length > 0)
					{
						_searchQuery = _searchQuery[..^1];
						ApplyFilter();
					}
					break;
				case ConsoleKey.Escape:
					return;
				default:
					if (!char.IsControl(keyInfo.KeyChar))
					{
						_searchQuery += keyInfo.KeyChar;
						ApplyFilter();
					}
					break;
			}
		}
	}

	private void Render()
	{
		AnsiConsole.Clear();
		Header.Render();

		RenderSearchBar();
		Console.SetCursorPosition(0, Console.CursorTop + 2);
		RenderKeyList();

		Console.WriteLine();
		RenderPagination();

		Console.WriteLine();
		RenderActionBar();

		StatusBar.Render(_config);

		Console.CursorTop = _searchBarRow;
		Console.CursorLeft = _searchEndCol;
	}

	private void RenderSearchBar()
	{
		var bgSeq = "\x1b[48;2;27;28;30m";
		var resetSeq = "\x1b[0m";
		var fill = new string(' ', Console.WindowWidth);

		Console.Write(bgSeq + fill + resetSeq);
		Console.WriteLine();

		Console.Write(bgSeq);

		if (_searchQuery.Length == 0)
			AnsiConsole.Markup("[grey]  \U0001f50d  Type to search...[/]");
		else
			AnsiConsole.Markup($"  \U0001f50d [white]{Markup.Escape(_searchQuery)}[/]");

		_searchEndCol = Console.CursorLeft;
		_searchBarRow = Console.CursorTop;

		var remaining = Console.WindowWidth - _searchEndCol;

		if (remaining > 0)
			Console.Write(bgSeq + new string(' ', remaining) + resetSeq);

		Console.WriteLine();

		Console.Write(bgSeq + fill + resetSeq);
	}

	private void RenderKeyList()
	{
		var pageKeys = GetCurrentPageKeys();

		if (_filteredKeys.Count == 0)
		{
			AnsiConsole.MarkupLine(NoKeysFound);

			return;
		}

		var keyWidth = KeyColumnWidth;
		var valueWidth = ValueColumnWidth;

		for (var i = 0; i < pageKeys.Count; i++)
		{
			var kv = pageKeys[i];
			var isSelected = i == _selectedIndex;

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

	private void RenderPagination()
	{
		var bgSeq = "\x1b[48;2;27;28;30m";
		var greySeq = "\x1b[38;2;128;128;128m";
		var whiteSeq = "\x1b[38;2;255;255;255m";
		var resetSeq = "\x1b[0m";
		var fill = new string(' ', Console.WindowWidth);

		var totalPages = GetTotalPages();
		var totalKeys = _filteredKeys.Count;
		var currentPageLabel = _currentPage + 1;

		Console.Write(bgSeq + fill + resetSeq);
		Console.WriteLine();

		Console.Write(bgSeq + greySeq + "  Page " + whiteSeq + currentPageLabel + "/" + totalPages + greySeq + "  •  " + whiteSeq + totalKeys + greySeq + " total keys" + resetSeq);
		var remaining = Console.WindowWidth - Console.CursorLeft;

		if (remaining > 0)
			Console.Write(bgSeq + new string(' ', remaining) + resetSeq);

		Console.WriteLine();
		Console.WriteLine(bgSeq + fill + resetSeq);
	}

	private void RenderActionBar()
	{
		if (!_showActions || _selectedKey is null)
			return;

		RenderSelectedPanel();
		RenderButtonsPanel();
	}

	private void RenderSelectedPanel()
	{
		var fill = new string(' ', Console.WindowWidth);
		var visible = $"  Selected: {_selectedKey!.Key}";

		Console.WriteLine($"{SelectedPanelBg}{fill}{Reset}");
		Console.WriteLine($"{SelectedPanelBg}{GreyFg}  Selected: {AccentFg}{_selectedKey.Key}{new string(' ', Math.Max(0, Console.WindowWidth - visible.Length))}{Reset}");
		Console.WriteLine($"{SelectedPanelBg}{fill}{Reset}");
	}

	private void RenderButtonsPanel()
	{
		var fill = new string(' ', Console.WindowWidth);
		var buttons = new (string Key, string Label)[] { ("E", "Edit"), ("D", "Delete"), ("Esc", "Cancel") };
		var visible = "  " + string.Join("   ", buttons.Select(b => $"{b.Key} {b.Label}"));
		var colored = "  " + string.Join("   ", buttons.Select(b => $"{WhiteFg}{b.Key} {GreyFg}{b.Label}"));

		Console.WriteLine($"{PanelBg}{fill}{Reset}");
		Console.WriteLine($"{PanelBg}{colored}{new string(' ', Math.Max(0, Console.WindowWidth - visible.Length))}{Reset}");
		Console.WriteLine($"{PanelBg}{fill}{Reset}");
	}

	private async Task EditKeyAsync()
	{
		if (_selectedKey is null)
			return;

		AnsiConsole.Clear();
		Header.Render();

		var savedTop = Console.CursorTop;

		StatusBar.Render(_config);
		Console.CursorTop = savedTop;
		Console.CursorLeft = 0;
		AnsiConsole.MarkupLine($"Editing key: {SelectionColor}{Markup.Escape(_selectedKey.Key)}[/]");
		AnsiConsole.MarkupLine($"Current value: [green]{Markup.Escape(TruncateText(_selectedKey.Value, EditValueMaxLength))}[/]");
		Console.WriteLine();

		var newValue = Prompt.Ask(EnterNewValue, _selectedKey.Value);

		if (newValue is null)
			return;

		var result = await _etcdClient.UpdateKeyAsync(_selectedKey.Key, newValue);

		AnsiConsole.Clear();
		Header.Render();

		savedTop = Console.CursorTop;

		StatusBar.Render(_config);
		Console.CursorTop = savedTop;
		Console.CursorLeft = 0;

		if (result)
		{
			AnsiConsole.MarkupLine(KeyUpdated);

			await ReloadAsync();
		}
		else
			AnsiConsole.MarkupLine(CouldNotUpdateKey);

		Console.WriteLine();
		AnsiConsole.Markup(Prompt.PressAnyKeyMarkup);
		Console.ReadKey(true);
	}

	private async Task DeleteKeyAsync()
	{
		if (_selectedKey is null)
			return;

		AnsiConsole.Clear();
		Header.Render();

		var savedTop = Console.CursorTop;

		StatusBar.Render(_config);
		Console.CursorTop = savedTop;
		Console.CursorLeft = 0;
		AnsiConsole.MarkupLine($"Delete key: [red]{Markup.Escape(_selectedKey.Key)}[/]");
		Console.WriteLine();

		var confirm = Prompt.Confirm(AreYouSure);

		if (confirm is not true)
			return;

		var result = await _etcdClient.DeleteKeyAsync(_selectedKey.Key);

		AnsiConsole.Clear();
		Header.Render();

		savedTop = Console.CursorTop;

		StatusBar.Render(_config);
		Console.CursorTop = savedTop;
		Console.CursorLeft = 0;

		if (result)
		{
			AnsiConsole.MarkupLine(KeyDeleted);

			await ReloadAsync();
		}
		else
			AnsiConsole.MarkupLine(KeyCouldNotBeDeleted);

		Console.WriteLine();
		AnsiConsole.Markup(Prompt.PressAnyKeyMarkup);
		Console.ReadKey(true);
	}

	private async Task ReloadAsync()
	{
		await LoadKeysAsync();

		_currentPage = Math.Min(_currentPage, GetTotalPages() - 1);

		if (_currentPage < 0)
			_currentPage = 0;

		_selectedIndex = 0;
	}

	private void ApplyFilter()
	{
		if (string.IsNullOrEmpty(_searchQuery))
			_filteredKeys = [.. _allKeys];
		else
		{
			var query = _searchQuery;

			_filteredKeys = [.. _allKeys
				.Where(kv =>
					kv.Key.Contains(query, StringComparison.OrdinalIgnoreCase) ||
					kv.Value.Contains(query, StringComparison.OrdinalIgnoreCase))];
		}

		_currentPage = 0;
		_selectedIndex = 0;
	}

	private List<EtcdKeyValue> GetCurrentPageKeys()
	{
		var start = _currentPage * PageSize;

		return [.. _filteredKeys.Skip(start).Take(PageSize)];
	}

	private EtcdKeyValue? GetCurrentPageKey()
	{
		var pageKeys = GetCurrentPageKeys();

		return _selectedIndex < pageKeys.Count ? pageKeys[_selectedIndex] : null;
	}

	private int GetPageItemCount() =>
		Math.Min(PageSize, _filteredKeys.Count - _currentPage * PageSize);

	private int GetTotalPages() =>
		_filteredKeys.Count == 0 ? 1 : (int)Math.Ceiling((double)_filteredKeys.Count / PageSize);

	private static string TruncateText(string text, int maxLength) =>
		text.Length <= maxLength ? text : text[..maxLength] + "...";
}
