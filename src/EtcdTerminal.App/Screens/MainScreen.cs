using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.Keys;
using EtcdTerminal.App.Screens.Permissions;
using EtcdTerminal.App.Screens.Roles;
using EtcdTerminal.App.Screens.Users;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Screens;

public sealed class MainScreen(
	ITerminal _terminal,
	IEtcdClient _etcdClient,
	KeyBrowseScreen _keyBrowse,
	KeyCreateScreen _keyCreate,
	KeyImportJsonScreen _keyImportJson,
	UserManagementScreen _userManagement,
	RoleManagementScreen _roleManagement,
	PermissionViewScreen _permissionView,
	Menu _menu)
{
	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		while (true)
		{
			_terminal.Clear();
			Header.Render(_terminal);

			MainMenuAction? action = _menu.Show<MainMenuAction>(
				"",
				[
					new(MainMenuAction.BrowseKeys, LocalizationStore.Current.BrowseKeys),
					new(MainMenuAction.CreateKey, LocalizationStore.Current.CreateKey),
					new(MainMenuAction.ImportJson, LocalizationStore.Current.ImportJson),
					new(MainMenuAction.ManageUsers, LocalizationStore.Current.ManageUsers),
					new(MainMenuAction.ManageRoles, LocalizationStore.Current.ManageRoles),
					new(MainMenuAction.ViewPermissions, LocalizationStore.Current.ViewPermissions),
					new(MainMenuAction.Disconnect, LocalizationStore.Current.Disconnect)
				],
				config: config)?.Id;

			if (action is null)
			{
				await _etcdClient.DisconnectAsync();
				return;
			}

			switch (action)
			{
				case MainMenuAction.BrowseKeys:
					await _keyBrowse.ShowAsync(config);
					break;
				case MainMenuAction.CreateKey:
					await _keyCreate.ShowAsync(config);
					break;
				case MainMenuAction.ImportJson:
					await _keyImportJson.ShowAsync(config);
					break;
				case MainMenuAction.ManageUsers:
					await _userManagement.ShowAsync(config);
					break;
				case MainMenuAction.ManageRoles:
					await _roleManagement.ShowAsync(config);
					break;
				case MainMenuAction.ViewPermissions:
					await _permissionView.ShowAsync(config);
					break;
				case MainMenuAction.Disconnect:
					await _etcdClient.DisconnectAsync();
					return;
			}
		}
	}
}
