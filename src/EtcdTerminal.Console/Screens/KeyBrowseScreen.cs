using EtcdTerminal.Console.Engine;
using EtcdTerminal.Console.Modules;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.Console.Screens;

public sealed class KeyBrowseScreen(IEtcdClient _etcdClient)
{
	private const int PageSize = 10;

	private List<EtcdKeyValue> _allKeys = [];
	private List<EtcdKeyValue> _filteredKeys = [];
	private string _searchQuery = "";
	private int _currentPage;
	private int _selectedIndex;
	private bool _showActions;
	private EtcdKeyValue? _selectedKey;
	private EtcdConnectionConfig _config = default!;

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

		if (!authEnabled)
		{
			_allKeys = [.. await _etcdClient.GetKeysByPrefixAsync("")];
		}
		else
		{
			var username = _config.Username;

			if (username is null)
			{
				_allKeys = [.. await _etcdClient.GetKeysByPrefixAsync("")];
				_filteredKeys = [.. _allKeys];

				return;
			}

			var user = await _etcdClient.GetUserAsync(username);

			if (user is null || user.Roles.Count == 0)
			{
				_allKeys = [.. await _etcdClient.GetKeysByPrefixAsync("")];
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

			var keyInfo = System.Console.ReadKey(true);

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
					var c = keyInfo.KeyChar;

					if (c >= ' ' && c <= '~')
					{
						_searchQuery += c;
						ApplyFilter();
					}
					break;
			}
		}
	}

	private void Render()
	{
		AnsiConsole.Clear();
		StatusBar.Render(_config);

		System.Console.Write("  ");
		RenderSearchBar();

		System.Console.WriteLine();
		RenderKeyList();

		System.Console.WriteLine();
		RenderPagination();

		RenderActionBar();
	}

	private void RenderSearchBar()
	{
		if (_searchQuery.Length == 0)
			AnsiConsole.Markup("[grey]🔍  Type to search keys...[/]");
		else
			AnsiConsole.Markup($"[yellow]🔍[/] [white]{Markup.Escape(_searchQuery)}[/]");
	}

	private void RenderKeyList()
	{
		var pageKeys = GetCurrentPageKeys();

		if (_filteredKeys.Count == 0)
		{
			AnsiConsole.MarkupLine("  [grey]No keys found.[/]");

			return;
		}

		for (var i = 0; i < pageKeys.Count; i++)
		{
			var kv = pageKeys[i];
			var isSelected = i == _selectedIndex;

			var prefix = isSelected ? " ▶ " : "    ";
			var key = TruncateText(kv.Key, 60);
			var value = TruncateText(kv.Value, 60);
			var line = $"{prefix}{key,-62} {value}";

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
			System.Console.WriteLine();

			return;
		}

		AnsiConsole.MarkupLine($"  [grey]Selected:[/] [cyan]{Markup.Escape(_selectedKey.Key)}[/]");
		AnsiConsole.Markup("  ");
		AnsiConsole.Markup("[bold yellow][[E]][/] [white]Edit[/]    ");
		AnsiConsole.Markup("[bold yellow][[D]][/] [white]Delete[/]    ");
		AnsiConsole.Markup("[bold yellow][[Esc]][/] [white]Cancel[/]");
		System.Console.WriteLine();
	}

	private async Task EditKeyAsync()
	{
		if (_selectedKey is null)
			return;

		AnsiConsole.Clear();
		StatusBar.Render(_config);
		AnsiConsole.MarkupLine($"Editing key: [cyan]{Markup.Escape(_selectedKey.Key)}[/]");
		AnsiConsole.MarkupLine($"Current value: [green]{Markup.Escape(TruncateText(_selectedKey.Value, 200))}[/]");
		System.Console.WriteLine();

		var newValue = Prompt.Ask("Enter new value:", _selectedKey.Value);

		if (newValue is null)
			return;

		var result = await _etcdClient.UpdateKeyAsync(_selectedKey.Key, newValue);

		AnsiConsole.Clear();
		StatusBar.Render(_config);

		if (result)
		{
			AnsiConsole.MarkupLine("[green]Key updated successfully![/]");
			await ReloadAsync();
		}
		else
		{
			AnsiConsole.MarkupLine("[red]Could not update key.[/]");
		}

		System.Console.WriteLine();
		AnsiConsole.Markup("[grey]Press any key to continue...[/]");
		System.Console.ReadKey(true);
	}

	private async Task DeleteKeyAsync()
	{
		if (_selectedKey is null)
			return;

		AnsiConsole.Clear();
		StatusBar.Render(_config);
		AnsiConsole.MarkupLine($"Delete key: [red]{Markup.Escape(_selectedKey.Key)}[/]");
		System.Console.WriteLine();

		var confirm = Prompt.Confirm("Are you sure?");

		if (confirm is not true)
			return;

		var result = await _etcdClient.DeleteKeyAsync(_selectedKey.Key);

		AnsiConsole.Clear();
		StatusBar.Render(_config);

		if (result)
		{
			AnsiConsole.MarkupLine("[green]Key deleted successfully![/]");
			await ReloadAsync();
		}
		else
		{
			AnsiConsole.MarkupLine("[red]Key could not be deleted.[/]");
		}

		System.Console.WriteLine();
		AnsiConsole.Markup("[grey]Press any key to continue...[/]");
		System.Console.ReadKey(true);
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
