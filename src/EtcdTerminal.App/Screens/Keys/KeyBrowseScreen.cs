using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;
using EtcdTerminal.Keys;
using EtcdTerminal.Permissions;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyBrowseScreen(ITerminal _terminal, IEtcdClient _etcdClient, ScreenLayout _screenLayout, KeyBrowseControl _control, PressAnyKeyPrompt _pressAnyKey)
{
	private const int EditValueMaxLength = 200;

	private List<EtcdKeyValue> _allKeys = [];
	private List<EtcdKeyValue> _filteredKeys = [];
	private EtcdConnectionConfig _config = default!;

	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		_config = config;

		await LoadKeysAsync();

		while (true)
		{
			_control.Render(GetCurrentPageKeys(), GetTotalPages(), _filteredKeys.Count, _config);

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
		_screenLayout.RenderHeader(_config);
		_terminal.Write($"{LocalizationStore.Current.EditingKey} ");
		_terminal.WriteLine(key.Key, TerminalColor.Default);
		_terminal.Write($"{LocalizationStore.Current.CurrentValue} ");
		_terminal.WriteLine(KeyBrowseLayout.TruncateText(key.Value, EditValueMaxLength), TerminalColor.Success);
		_terminal.WriteLine();

		var newValue = Prompt.Ask(_terminal, LocalizationStore.Current.EnterNewValue, key.Value);

		if (newValue is null)
			return;

		var result = await _etcdClient.UpdateKeyAsync(key.Key, newValue);

		_screenLayout.RenderHeader(_config);

		if (result)
		{
			_terminal.WriteLine(LocalizationStore.Current.KeyUpdated, TerminalColor.Success);

			await ReloadAsync();
		}
		else
			_terminal.WriteLine(LocalizationStore.Current.CouldNotUpdateKey, TerminalColor.Error);

		_terminal.WriteLine();
		_pressAnyKey.Show();
	}

	private async Task DeleteKeyAsync(EtcdKeyValue key)
	{
		_screenLayout.RenderHeader(_config);
		_terminal.Write($"{LocalizationStore.Current.DeleteKey} ");
		_terminal.WriteLine(key.Key, TerminalColor.Error);
		_terminal.WriteLine();

		if (!Prompt.Confirm(_terminal, LocalizationStore.Current.AreYouSure))
			return;

		var result = await _etcdClient.DeleteKeyAsync(key.Key);

		_screenLayout.RenderHeader(_config);

		if (result)
		{
			_terminal.WriteLine(LocalizationStore.Current.KeyDeleted, TerminalColor.Success);

			await ReloadAsync();
		}
		else
			_terminal.WriteLine(LocalizationStore.Current.KeyCouldNotBeDeleted, TerminalColor.Error);

		_terminal.WriteLine();
		_pressAnyKey.Show();
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
