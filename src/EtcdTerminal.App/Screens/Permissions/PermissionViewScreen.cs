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
		ScreenLayout.RenderHeader(config);

		await AnsiConsole.Status()
			.StartAsync(LoadingPermissions, async ctx =>
			{
				var users = await _etcdClient.GetUsersAsync();
				var roles = await _etcdClient.GetRolesAsync();

				PermissionViewRenderer.Render(users, roles);
			});

		PressAnyKeyPrompt.Show();
	}
}
