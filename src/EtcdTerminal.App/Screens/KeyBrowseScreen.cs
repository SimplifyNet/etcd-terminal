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
	private const int SearchBarRow = 4;
	private const int EditValueMaxLength = 200;

	private const string SearchPlaceholder = "[grey]🔍  Type to search keys...[/]";
	private const string NoKeysFound = "  [grey]No keys found.[/]";
	private const string EditAction = "[bold yellow][[E]][/] [white]Edit[/]    ";
	private const string DeleteAction = "[bold yellow][[D]][/] [white]Delete[/]    ";
	private const string CancelAction = "[bold yellow][[Esc]][/] [white]Cancel[/]";
	private const string KeyUpdated = "[green]Key updated successfully![/]";
	private const string CouldNotUpdateKey = "[red]Could not update key.[/]";
	private const string KeyDeleted = "[green]Key deleted successfully![/]";
	private const string KeyCouldNotBeDeleted = "[red]Key could not be deleted.[/]";
	private const string EnterNewValue = "Enter new value:";
	private const string AreYouSure = "Are you sure?";

	private static int KeyColumnWidth => (Console.WindowWidth - LinePadding - PrefixWidth - 1) / 2;
	private static int ValueColumnWidth => Console.WindowWidth - LinePadding - PrefixWidth - 1 - KeyColumnWidth;

	private List<EtcdKeyValue> _allKeys = [];
	private List<EtcdKeyValue> _filteredKeys = [];
	private string _searchQuery = "";
	private int _currentPage;
	private int _selectedIndex;
	private bool _showActions;
	private EtcdKeyValue? _selectedKey;
	private EtcdConnectionConfig _config = default!;
	private int _searchEndCol;

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
		{
			_allKeys = await LoadAllKeysAsync();
		}
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
			{
				_allKeys = await LoadAllKeysAsync();
			}
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

		Console.Write(new string(' ', LinePadding));
		RenderSearchBar();
		_searchEndCol = Console.CursorLeft;

		Console.WriteLine();
		RenderKeyList();

		Console.WriteLine();
		RenderPagination();

		RenderActionBar();

		StatusBar.Render(_config);

		Console.CursorTop = SearchBarRow;
		Console.CursorLeft = _searchEndCol;
	}

	private void RenderSearchBar()
	{
		if (_searchQuery.Length == 0)
			AnsiConsole.Markup(SearchPlaceholder);
		else
			AnsiConsole.Markup($"[yellow]🔍[/] [white]{Markup.Escape(_searchQuery)}[/]");
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

			var prefix = isSelected ? " ▶ " : "    ";
			var key = TruncateText(kv.Key, keyWidth);
			var value = TruncateText(kv.Value, valueWidth);
			var line = $"{prefix}{key.PadRight(keyWidth)} {value}";

			if (isSelected)
				AnsiConsole.MarkupLine($"  [cyan]{Markup.Escape(line)}[/]");
			else
				AnsiConsole.MarkupLine($"  [white]{Markup.Escape(line)}[/]");
		}
	}

	private void RenderPagination()
	{
		var totalPages = GetTotalPages();
		var totalKeys = _filteredKeys.Count;
		var currentPageLabel = _currentPage + 1;

		AnsiConsole.MarkupLine($"  [grey]Page {currentPageLabel}/{totalPages}  •  {totalKeys} total keys[/]");
	}

	private void RenderActionBar()
	{
		if (!_showActions || _selectedKey is null)
		{
			Console.WriteLine();

			return;
		}

		AnsiConsole.MarkupLine($"  [grey]Selected:[/] [cyan]{Markup.Escape(_selectedKey.Key)}[/]");
		AnsiConsole.Markup("  ");
		AnsiConsole.Markup(EditAction);
		AnsiConsole.Markup(DeleteAction);
		AnsiConsole.Markup(CancelAction);
		Console.WriteLine();
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
		AnsiConsole.MarkupLine($"Editing key: [cyan]{Markup.Escape(_selectedKey.Key)}[/]");
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
		{
			AnsiConsole.MarkupLine(CouldNotUpdateKey);
		}

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
		{
			AnsiConsole.MarkupLine(KeyCouldNotBeDeleted);
		}

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
		{
			_filteredKeys = [.. _allKeys];
		}
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

		return _filteredKeys.Skip(start).Take(PageSize).ToList();
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
