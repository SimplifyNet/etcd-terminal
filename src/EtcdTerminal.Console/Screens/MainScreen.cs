using EtcdTerminal.Console.Engine;
using EtcdTerminal.Console.Modules;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.Console.Screens;

public sealed class MainScreen(
	IEtcdClient _etcdClient,
	KeyBrowserScreen _keyBrowser,
	KeySearchScreen _keySearch,
	KeyCreateScreen _keyCreate,
	KeyEditScreen _keyEdit,
	UserManagementScreen _userManagement,
	RoleManagementScreen _roleManagement,
	PermissionViewScreen _permissionView)
{
	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		while (true)
		{
			AnsiConsole.Clear();
			StatusBar.Render(config);

			var choice = Menu.Show(
				$"Connected to: {config.Name}",
				new[]
				{
					"Browse Keys",
					"Search Keys",
					"Create Key",
					"Edit Key",
					"Delete Key",
					"Manage Users",
					"Manage Roles",
					"View Permissions",
					"Disconnect"
				});

			if (choice is null)
			{
				await _etcdClient.DisconnectAsync();
				return;
			}

			switch (choice)
			{
				case "Browse Keys":
					await _keyBrowser.ShowAsync(config);
					break;
				case "Search Keys":
					await _keySearch.ShowAsync(config);
					break;
				case "Create Key":
					await _keyCreate.ShowAsync(config);
					break;
				case "Edit Key":
					await _keyEdit.ShowAsync(config);
					break;
				case "Delete Key":
					await DeleteKeyAsync();
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

	private async Task DeleteKeyAsync()
	{
		var key = Prompt.Ask("Enter key to delete:");

		if (key is null)
			return;

		var confirm = Prompt.Confirm($"Are you sure you want to delete {key}?");

		if (confirm is not true)
			return;

		var result = await _etcdClient.DeleteKeyAsync(key);

		if (result)
			AnsiConsole.MarkupLine("[green]Key deleted successfully![/]");
		else
			AnsiConsole.MarkupLine("[red]Key not found or could not be deleted.[/]");

		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		System.Console.ReadKey(true);
	}
}
