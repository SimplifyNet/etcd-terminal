using EtcdTerminal;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Screens;

public sealed class MainScreen(
	IEtcdClient _etcdClient,
	KeyBrowseScreen _keyBrowse,
	KeyCreateScreen _keyCreate,
	UserManagementScreen _userManagement,
	RoleManagementScreen _roleManagement,
	PermissionViewScreen _permissionView)
{
	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		while (true)
		{
			AnsiConsole.Clear();
			Header.Render();

			var choice = Menu.Show(
				"",
				new[]
				{
					"Browse Keys",
					"Create Key",
					"Manage Users",
					"Manage Roles",
					"View Permissions",
					"Disconnect"
				},
				config: config);

			if (choice is null)
			{
				await _etcdClient.DisconnectAsync();
				return;
			}

			switch (choice)
			{
				case "Browse Keys":
					await _keyBrowse.ShowAsync(config);
					break;
				case "Create Key":
					await _keyCreate.ShowAsync(config);
					break;
				case "Manage Users":
					await _userManagement.ShowAsync(config);
					break;
				case "Manage Roles":
					await _roleManagement.ShowAsync(config);
					break;
				case "View Permissions":
					await _permissionView.ShowAsync(config);
					break;
				case "Disconnect":
					await _etcdClient.DisconnectAsync();
					return;
			}
		}
	}
}
