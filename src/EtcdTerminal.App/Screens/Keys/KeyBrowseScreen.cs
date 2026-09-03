using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using Spectre.Console;
using EtcdTerminal.Keys;
using EtcdTerminal.Permissions;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyBrowseScreen(IEtcdClient _etcdClient)
{
	private const int EditValueMaxLength = 200;

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
		AnsiConsole.MarkupLine($"{LocalizationStore.Current.EditingKey} {KeyBrowseLayout.SelectionColor}{Markup.Escape(key.Key)}[/]");
		AnsiConsole.MarkupLine($"{LocalizationStore.Current.CurrentValue} [green]{Markup.Escape(KeyBrowseLayout.TruncateText(key.Value, EditValueMaxLength))}[/]");
		AnsiConsole.WriteLine();

		var newValue = Prompt.Ask(LocalizationStore.Current.EnterNewValue, key.Value);

		if (newValue is null)
			return;

		var result = await _etcdClient.UpdateKeyAsync(key.Key, newValue);

		ScreenLayout.RenderHeader(_config);

		if (result)
		{
			AnsiConsole.MarkupLine(LocalizationStore.Current.KeyUpdated);

			await ReloadAsync();
		}
		else
			AnsiConsole.MarkupLine(LocalizationStore.Current.CouldNotUpdateKey);

		AnsiConsole.WriteLine();
		PressAnyKeyPrompt.Show();
	}

	private async Task DeleteKeyAsync(EtcdKeyValue key)
	{
		ScreenLayout.RenderHeader(_config);
		AnsiConsole.MarkupLine($"{LocalizationStore.Current.DeleteKey} [red]{Markup.Escape(key.Key)}[/]");
		AnsiConsole.WriteLine();

		if (!Prompt.Confirm(LocalizationStore.Current.AreYouSure))
			return;

		var result = await _etcdClient.DeleteKeyAsync(key.Key);

		ScreenLayout.RenderHeader(_config);

		if (result)
		{
			AnsiConsole.MarkupLine(LocalizationStore.Current.KeyDeleted);

			await ReloadAsync();
		}
		else
			AnsiConsole.MarkupLine(LocalizationStore.Current.KeyCouldNotBeDeleted);

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
		var pageSize = AppSettingsStore.Current.PageSize;
		var start = _control.CurrentPage * pageSize;

		return [.. _filteredKeys.Skip(start).Take(pageSize)];
	}

	private int GetTotalPages() =>
		_filteredKeys.Count == 0 ? 1 : (int)Math.Ceiling((double)_filteredKeys.Count / AppSettingsStore.Current.PageSize);
}