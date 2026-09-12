using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;
using EtcdTerminal.Permissions;

namespace EtcdTerminal.App.Screens.Roles;

public sealed class RoleManagementScreen(ITerminal _terminal, IEtcdClient _etcdClient, MenuScreen _menuScreen, PermissionTypeSelector _permissionTypeSelector, PressAnyKeyPrompt _pressAnyKey, Prompt _prompt)
{
	public async Task ShowAsync(EtcdConnectionConfig config) =>
		await _menuScreen.RunAsync(LocalizationStore.Current.RoleManagement, [LocalizationStore.Current.ListRoles, LocalizationStore.Current.CreateRole, LocalizationStore.Current.DeleteRole, LocalizationStore.Current.GrantPermission, LocalizationStore.Current.RevokePermission], config, HandleChoiceAsync);

	private async Task HandleChoiceAsync(string choice)
	{
		switch (choice)
		{
			case var _ when choice == LocalizationStore.Current.ListRoles:
				await ListRolesAsync();
				break;
			case var _ when choice == LocalizationStore.Current.CreateRole:
				await CreateRoleAsync();
				break;
			case var _ when choice == LocalizationStore.Current.DeleteRole:
				await DeleteRoleAsync();
				break;
			case var _ when choice == LocalizationStore.Current.GrantPermission:
				await GrantPermissionAsync();
				break;
			case var _ when choice == LocalizationStore.Current.RevokePermission:
				await RevokePermissionAsync();
				break;
		}
	}

	private async Task ListRolesAsync()
	{
		var roles = await _etcdClient.GetRolesAsync();

		RoleListRenderer.Render(_terminal, roles);

		_terminal.WriteLine();
		_pressAnyKey.Show();
	}

	private async Task CreateRoleAsync()
	{
		var roleName = _prompt.Ask(LocalizationStore.Current.EnterRoleNamePrompt);

		if (roleName is null)
			return;

		var result = await _etcdClient.CreateRoleAsync(roleName);

		_terminal.WriteLine();

		if (result)
			_terminal.WriteIndentedLine(LocalizationStore.Current.RoleCreated, TerminalColor.Success);
		else
			_terminal.WriteIndentedLine(LocalizationStore.Current.FailedCreateRole, TerminalColor.Error);

		_terminal.WriteLine();
		_pressAnyKey.Show();
	}

	private async Task DeleteRoleAsync()
	{
		var roleName = _prompt.Ask(LocalizationStore.Current.EnterRoleNameToDelete);

		if (roleName is null)
			return;

		var result = await _etcdClient.DeleteRoleAsync(roleName);

		_terminal.WriteLine();

		if (result)
			_terminal.WriteIndentedLine(LocalizationStore.Current.RoleDeleted, TerminalColor.Success);
		else
			_terminal.WriteIndentedLine(LocalizationStore.Current.FailedDeleteRole, TerminalColor.Error);

		_terminal.WriteLine();
		_pressAnyKey.Show();
	}

	private Task GrantPermissionAsync() =>
		GrantOrRevokeAsync((roleName, permType, keyPrefix) => _etcdClient.GrantPermissionAsync(roleName, permType, keyPrefix), LocalizationStore.Current.PermissionGranted, LocalizationStore.Current.FailedGrantPermission);

	private Task RevokePermissionAsync() =>
		GrantOrRevokeAsync((roleName, permType, keyPrefix) => _etcdClient.RevokePermissionAsync(roleName, permType, keyPrefix), LocalizationStore.Current.PermissionRevoked, LocalizationStore.Current.FailedRevokePermission);

	private async Task GrantOrRevokeAsync(Func<string, PermissionType, string, Task> action, string successMessage, string failureMessage)
	{
		var roleName = _prompt.Ask(LocalizationStore.Current.EnterRoleNamePrompt);

		if (roleName is null)
			return;

		var keyPrefix = _prompt.Ask(LocalizationStore.Current.EnterKeyPrefix);

		if (keyPrefix is null)
			return;

		var permType = _permissionTypeSelector.Select();

		if (permType is null)
			return;

		try
		{
			await action(roleName, permType.Value, keyPrefix);

			_terminal.WriteLine();
			_terminal.WriteIndentedLine(successMessage, TerminalColor.Success);
		}
		catch
		{
			_terminal.WriteLine();
			_terminal.WriteIndentedLine(failureMessage, TerminalColor.Error);
		}

		_terminal.WriteLine();
		_pressAnyKey.Show();
	}
}
