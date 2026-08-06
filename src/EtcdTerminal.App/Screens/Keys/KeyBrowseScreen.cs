using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyBrowseScreen(IEtcdClient _etcdClient)
{
	private const int PageSize = 30;
	private const int EditValueMaxLength = 200;

	private const string KeyUpdated = "[green]Key updated successfully![/]";
	private const string CouldNotUpdateKey = "[red]Could not update key.[/]";
	private const string KeyDeleted = "[green]Key deleted successfully![/]";
	private const string KeyCouldNotBeDeleted = "[red]Key could not be deleted.[/]";
	private const string EnterNewValue = "Enter new value:";
	private const string AreYouSure = "Are you sure?";

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

		var (searchEndCol, searchBarRow) = KeyBrowseLayout.RenderSearchBar(_searchQuery);
		_searchEndCol = searchEndCol;
		_searchBarRow = searchBarRow;

		Console.SetCursorPosition(0, Console.CursorTop + 2);
		KeyBrowseLayout.RenderKeyList(GetCurrentPageKeys(), _selectedIndex);

		Console.WriteLine();
		KeyBrowseLayout.RenderPagination(_currentPage, GetTotalPages(), _filteredKeys.Count);

		Console.WriteLine();
		if (_showActions && _selectedKey is not null)
			KeyBrowseLayout.RenderActionBar(_selectedKey.Key);

		StatusBar.Render(_config);

		Console.CursorTop = _searchBarRow;
		Console.CursorLeft = _searchEndCol;
	}

	private async Task EditKeyAsync()
	{
		if (_selectedKey is null)
			return;

		ScreenLayout.RenderHeader(_config);
		AnsiConsole.MarkupLine($"Editing key: {KeyBrowseLayout.SelectionColor}{Markup.Escape(_selectedKey.Key)}[/]");
		AnsiConsole.MarkupLine($"Current value: [green]{Markup.Escape(KeyBrowseLayout.TruncateText(_selectedKey.Value, EditValueMaxLength))}[/]");
		Console.WriteLine();

		var newValue = Prompt.Ask(EnterNewValue, _selectedKey.Value);

		if (newValue is null)
			return;

		var result = await _etcdClient.UpdateKeyAsync(_selectedKey.Key, newValue);

		ScreenLayout.RenderHeader(_config);

		if (result)
		{
			AnsiConsole.MarkupLine(KeyUpdated);

			await ReloadAsync();
		}
		else
			AnsiConsole.MarkupLine(CouldNotUpdateKey);

		Console.WriteLine();
		PressAnyKeyPrompt.Show();
	}

	private async Task DeleteKeyAsync()
	{
		if (_selectedKey is null)
			return;

		ScreenLayout.RenderHeader(_config);
		AnsiConsole.MarkupLine($"Delete key: [red]{Markup.Escape(_selectedKey.Key)}[/]");
		Console.WriteLine();

		var confirm = Prompt.Confirm(AreYouSure);

		if (confirm is not true)
			return;

		var result = await _etcdClient.DeleteKeyAsync(_selectedKey.Key);

		ScreenLayout.RenderHeader(_config);

		if (result)
		{
			AnsiConsole.MarkupLine(KeyDeleted);

			await ReloadAsync();
		}
		else
			AnsiConsole.MarkupLine(KeyCouldNotBeDeleted);

		Console.WriteLine();
		PressAnyKeyPrompt.Show();
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
}
