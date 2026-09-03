using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.Keys;
using EtcdTerminal.App.Screens.Permissions;
using EtcdTerminal.App.Screens.Roles;
using EtcdTerminal.App.Screens.Users;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using Spectre.Console;

namespace EtcdTerminal.App.Screens;

public sealed class MainScreen(
	IEtcdClient _etcdClient,
	KeyBrowseScreen _keyBrowse,
	KeyCreateScreen _keyCreate,
	UserManagementScreen _userManagement,
	RoleManagementScreen _roleManagement,
	PermissionViewScreen _permissionView,
	Menu _menu)
{
	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		while (true)
		{
			AnsiConsole.Clear();
			Header.Render();

			var choice = _menu.Show(
				"",
				[
					LocalizationStore.Current.BrowseKeys,
					LocalizationStore.Current.CreateKey,
					LocalizationStore.Current.ManageUsers,
					LocalizationStore.Current.ManageRoles,
					LocalizationStore.Current.ViewPermissions,
					LocalizationStore.Current.Disconnect
				],
				config: config);

			if (choice is null)
			{
				await _etcdClient.DisconnectAsync();
				return;
			}

			switch (choice)
			{
				case var _ when choice == LocalizationStore.Current.BrowseKeys:
					await _keyBrowse.ShowAsync(config);
					break;
				case var _ when choice == LocalizationStore.Current.CreateKey:
					await _keyCreate.ShowAsync(config);
					break;
				case var _ when choice == LocalizationStore.Current.ManageUsers:
					await _userManagement.ShowAsync(config);
					break;
				case var _ when choice == LocalizationStore.Current.ManageRoles:
					await _roleManagement.ShowAsync(config);
					break;
				case var _ when choice == LocalizationStore.Current.ViewPermissions:
					await _permissionView.ShowAsync(config);
					break;
				case var _ when choice == LocalizationStore.Current.Disconnect:
					await _etcdClient.DisconnectAsync();
					return;
			}
		}
	}
}
