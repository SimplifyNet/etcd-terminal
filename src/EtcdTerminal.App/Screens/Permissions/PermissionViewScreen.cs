using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using EtcdTerminal.Roles;
using EtcdTerminal.Terminal;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Permissions;

public sealed class PermissionViewScreen(ITerminal _terminal, IEtcdClient _etcdClient, ScreenLayout _screenLayout, PressAnyKeyPrompt _pressAnyKey, Spinner _spinner, Message _message)
{
	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		_screenLayout.RenderHeader(config);

		IReadOnlyList<EtcdUser> users = [];
		IReadOnlyList<EtcdRole> roles = [];

		var loaded = await _spinner.RunAsync(LocalizationStore.Current.LoadingPermissions, async ct =>
		{
			users = await _etcdClient.GetUsersAsync(ct);
			roles = await _etcdClient.GetRolesAsync(ct);
		});

		if (!loaded)
		{
			_message.ShowWarning(LocalizationStore.Current.OperationCancelled);

			return;
		}

		PermissionViewRenderer.Render(_terminal, users, roles);

		_terminal.WriteLine();
		_pressAnyKey.Show();
	}
}
