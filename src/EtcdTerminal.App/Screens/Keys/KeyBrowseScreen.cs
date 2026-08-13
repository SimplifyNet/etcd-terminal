using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyBrowseScreen(IEtcdClient _etcdClient)
{
	private const int EditValueMaxLength = 200;

	private const string KeyUpdated = "[green]Key updated successfully![/]";
	private const string CouldNotUpdateKey = "[red]Could not update key.[/]";
	private const string KeyDeleted = "[green]Key deleted successfully![/]";
	private const string KeyCouldNotBeDeleted = "[red]Key could not be deleted.[/]";
	private const string EnterNewValue = "Enter new value:";
	private const string AreYouSure = "Are you sure?";

	private List<EtcdKeyValue> _allKeys = [];
	private List<EtcdKeyValue> _filteredKeys = [];
	private EtcdConnectionConfig _config = default!;
	private KeyBrowseControl _control = default!;

	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		_config = config;
		_control = new KeyBrowseControl(config);

		await LoadKeysAsync();

		while (true)
		{
			_control.Render(GetCurrentPageKeys(), GetTotalPages(), _filteredKeys.Count);

			var command = _control.ReadCommand(GetCurrentPageKeys(), GetTotalPages());

			switch (command.Action)
			{
				case KeyBrowseAction.SearchChanged:
					ApplyFilter();
					break;
				case KeyBrowseAction.Edit:
					await EditKeyAsync(command.SelectedKey!);
					break;
				case KeyBrowseAction.Delete:
					await DeleteKeyAsync(command.SelectedKey!);
					break;
				case KeyBrowseAction.Exit:
					return;
			}
		}
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

	private async Task EditKeyAsync(EtcdKeyValue key)
	{
		ScreenLayout.RenderHeader(_config);
		AnsiConsole.MarkupLine($"Editing key: {KeyBrowseLayout.SelectionColor}{Markup.Escape(key.Key)}[/]");
		AnsiConsole.MarkupLine($"Current value: [green]{Markup.Escape(KeyBrowseLayout.TruncateText(key.Value, EditValueMaxLength))}[/]");
		AnsiConsole.WriteLine();

		var newValue = Prompt.Ask(EnterNewValue, key.Value);

		if (newValue is null)
			return;

		var result = await _etcdClient.UpdateKeyAsync(key.Key, newValue);

		ScreenLayout.RenderHeader(_config);

		if (result)
		{
			AnsiConsole.MarkupLine(KeyUpdated);

			await ReloadAsync();
		}
		else
			AnsiConsole.MarkupLine(CouldNotUpdateKey);

		AnsiConsole.WriteLine();
		PressAnyKeyPrompt.Show();
	}

	private async Task DeleteKeyAsync(EtcdKeyValue key)
	{
		ScreenLayout.RenderHeader(_config);
		AnsiConsole.MarkupLine($"Delete key: [red]{Markup.Escape(key.Key)}[/]");
		AnsiConsole.WriteLine();

		if (!Prompt.Confirm(AreYouSure))
			return;

		var result = await _etcdClient.DeleteKeyAsync(key.Key);

		ScreenLayout.RenderHeader(_config);

		if (result)
		{
			AnsiConsole.MarkupLine(KeyDeleted);

			await ReloadAsync();
		}
		else
			AnsiConsole.MarkupLine(KeyCouldNotBeDeleted);

		AnsiConsole.WriteLine();
		PressAnyKeyPrompt.Show();
	}

	private async Task ReloadAsync()
	{
		await LoadKeysAsync();

		_control.ClampPage(GetTotalPages());
	}

	private void ApplyFilter()
	{
		if (string.IsNullOrEmpty(_control.SearchQuery))
			_filteredKeys = [.. _allKeys];
		else
		{
			var query = _control.SearchQuery;

			_filteredKeys = [.. _allKeys
				.Where(kv =>
					kv.Key.Contains(query, StringComparison.OrdinalIgnoreCase) ||
					kv.Value.Contains(query, StringComparison.OrdinalIgnoreCase))];
		}

		_control.ResetNavigation();
	}

	private List<EtcdKeyValue> GetCurrentPageKeys()
	{
		var pageSize = EtcdTerminalSettings.PageSize;
		var start = _control.CurrentPage * pageSize;

		return [.. _filteredKeys.Skip(start).Take(pageSize)];
	}

	private int GetTotalPages() =>
		_filteredKeys.Count == 0 ? 1 : (int)Math.Ceiling((double)_filteredKeys.Count / EtcdTerminalSettings.PageSize);
}
