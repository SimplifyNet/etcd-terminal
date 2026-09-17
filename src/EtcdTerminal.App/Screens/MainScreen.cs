using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.Keys;
using EtcdTerminal.App.Screens.Permissions;
using EtcdTerminal.App.Screens.Roles;
using EtcdTerminal.App.Screens.Users;
using EtcdTerminal.Configuration;
using EtcdTerminal.Session;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;

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

			MainMenuAction? action = _menu.Show(string.Empty, BuildMenuItems())?.Id;

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

	/// <summary>
	/// Only shows the actions the connected account is actually permitted to perform.
	/// </summary>
	private List<MenuItem<MainMenuAction>> BuildMenuItems()
	{
		var capabilities = _session.Capabilities;

		List<MenuItem<MainMenuAction>> items = [];

		if (capabilities.CanReadKeys)
			items.Add(new(MainMenuAction.BrowseKeys, LocalizationStore.Current.BrowseKeys));

		if (capabilities.CanWriteKeys)
		{
			items.Add(new(MainMenuAction.CreateKey, LocalizationStore.Current.CreateKey));
			items.Add(new(MainMenuAction.ImportJson, LocalizationStore.Current.ImportJson));
		}

		if (capabilities.CanManageAuth)
		{
			items.Add(new(MainMenuAction.ManageUsers, LocalizationStore.Current.ManageUsers));
			items.Add(new(MainMenuAction.ManageRoles, LocalizationStore.Current.ManageRoles));
			items.Add(new(MainMenuAction.ViewPermissions, LocalizationStore.Current.ViewPermissions));
		}

		items.Add(new(MainMenuAction.Disconnect, LocalizationStore.Current.Disconnect));

		return items;
	}
}
