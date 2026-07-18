using EtcdTerminal.Console.Helpers;
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
		var running = true;

		while (running)
		{
			AnsiConsole.Clear();
			Helpers.StatusBar.Render(config);

			var choice = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title($"[bold]Connected to:[/] [cyan]{config.Name}[/]")
					.PageSize(10)
					.AddChoices(
						"Browse Keys",
						"Search Keys",
						"Create Key",
						"Edit Key",
						"Delete Key",
						"Manage Users",
						"Manage Roles",
						"View Permissions",
						"Disconnect"));

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
					running = false;
					break;
			}
		}
	}

	private async Task DeleteKeyAsync()
	{
		var key = AnsiConsole.Ask<string>("Enter key to delete:");

		if (!AnsiConsole.Confirm($"Are you sure you want to delete [red]{key}[/]?"))
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
