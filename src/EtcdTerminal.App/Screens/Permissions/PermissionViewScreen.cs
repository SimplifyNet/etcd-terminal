using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using Spectre.Console;

namespace EtcdTerminal.App.Screens.Permissions;

public sealed class PermissionViewScreen(IEtcdClient _etcdClient)
{
	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		ScreenLayout.RenderHeader(config);

		await AnsiConsole.Status()
			.StartAsync(LocalizationStore.Current.LoadingPermissions, async ctx =>
			{
				var users = await _etcdClient.GetUsersAsync();
				var roles = await _etcdClient.GetRolesAsync();

				PermissionViewRenderer.Render(users, roles);
			});

		PressAnyKeyPrompt.Show();
	}
}