using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.Keys;
using EtcdTerminal.App.Screens.Permissions;
using EtcdTerminal.App.Screens.Roles;
using EtcdTerminal.App.Screens.Users;
using EtcdTerminal.Configuration;
using EtcdTerminal.Session;
using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Screens;

public sealed class MainScreen(
	ScreenLayout _screenLayout,
	IEtcdConnection _connection,
	IConnectionSession _session,
	KeyBrowseScreen _keyBrowse,
	KeyCreateScreen _keyCreate,
	KeyImportJsonScreen _keyImportJson,
	UserManagementScreen _userManagement,
	RoleManagementScreen _roleManagement,
	PermissionViewScreen _permissionView,
	Menu _menu)
{
	public async Task ShowAsync()
	{
		while (true)
		{
			_screenLayout.RenderHeader();

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
				])?.Id;

			if (action is null)
			{
				_session.End();
				await _connection.DisconnectAsync();
				return;
			}

			switch (action)
			{
				case MainMenuAction.BrowseKeys:
					await _keyBrowse.ShowAsync();
					break;
				case MainMenuAction.CreateKey:
					await _keyCreate.ShowAsync();
					break;
				case MainMenuAction.ImportJson:
					await _keyImportJson.ShowAsync();
					break;
				case MainMenuAction.ManageUsers:
					await _userManagement.ShowAsync();
					break;
				case MainMenuAction.ManageRoles:
					await _roleManagement.ShowAsync();
					break;
				case MainMenuAction.ViewPermissions:
					await _permissionView.ShowAsync();
					break;
				case MainMenuAction.Disconnect:
					_session.End();
					await _connection.DisconnectAsync();
					return;
			}
		}
	}
}
