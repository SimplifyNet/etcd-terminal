using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Screens.Permissions;

public sealed class PermissionViewScreen(ITerminal _terminal, IEtcdClient _etcdClient, ScreenLayout _screenLayout, PressAnyKeyPrompt _pressAnyKey)
{
	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		_screenLayout.RenderHeader(config);

		await _terminal.ShowStatusAsync(LocalizationStore.Current.LoadingPermissions, async ct =>
		{
			var users = await _etcdClient.GetUsersAsync();
			var roles = await _etcdClient.GetRolesAsync();

			PermissionViewRenderer.Render(_terminal, users, roles);
		});

		_pressAnyKey.Show();
	}
}
