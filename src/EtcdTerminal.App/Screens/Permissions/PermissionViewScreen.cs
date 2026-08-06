using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Screens.Permissions;

public sealed class PermissionViewScreen(IEtcdClient _etcdClient)
{
	private const string LoadingPermissions = "Loading permissions...";

	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		AnsiConsole.Clear();
		Header.Render();
		var savedTop = Console.CursorTop;
		StatusBar.Render(config);
		Console.CursorTop = savedTop;
		Console.CursorLeft = 0;

		await AnsiConsole.Status()
			.StartAsync(LoadingPermissions, async ctx =>
			{
				var users = await _etcdClient.GetUsersAsync();
				var roles = await _etcdClient.GetRolesAsync();

				PermissionViewRenderer.Render(users, roles);
			});

		AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
		Console.ReadKey(true);
	}
}
