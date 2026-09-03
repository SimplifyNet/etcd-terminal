using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using Spectre.Console;
using EtcdTerminal.Permissions;

namespace EtcdTerminal.App.Screens.Roles;

public sealed class RoleManagementScreen(IEtcdClient _etcdClient)
{
	public async Task ShowAsync(EtcdConnectionConfig config) =>
		await MenuScreen.RunAsync(LocalizationStore.Current.RoleManagement, [LocalizationStore.Current.ListRoles, LocalizationStore.Current.CreateRole, LocalizationStore.Current.DeleteRole, LocalizationStore.Current.GrantPermission, LocalizationStore.Current.RevokePermission], config, HandleChoiceAsync);

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

		RoleListRenderer.Render(roles);

		PressAnyKeyPrompt.Show();
	}

	private async Task CreateRoleAsync()
	{
		var roleName = Prompt.Ask(LocalizationStore.Current.EnterRoleNamePrompt);

		if (roleName is null)
			return;

		var result = await _etcdClient.CreateRoleAsync(roleName);

		if (result)
			AnsiConsole.MarkupLine(LocalizationStore.Current.RoleCreated);
		else
			AnsiConsole.MarkupLine(LocalizationStore.Current.FailedCreateRole);

		PressAnyKeyPrompt.Show();
	}

	private async Task DeleteRoleAsync()
	{
		var roleName = Prompt.Ask(LocalizationStore.Current.EnterRoleNameToDelete);

		if (roleName is null)
			return;

		var confirm = Prompt.Confirm(string.Format(LocalizationStore.Current.DeleteRoleConfirm, roleName));

		if (confirm is not true)
			return;

		var result = await _etcdClient.DeleteRoleAsync(roleName);

		if (result)
			AnsiConsole.MarkupLine(LocalizationStore.Current.RoleDeleted);
		else
			AnsiConsole.MarkupLine(LocalizationStore.Current.FailedDeleteRole);

		PressAnyKeyPrompt.Show();
	}

	private Task GrantPermissionAsync() =>
		GrantOrRevokeAsync((roleName, permType, keyPrefix) => _etcdClient.GrantPermissionAsync(roleName, permType, keyPrefix), LocalizationStore.Current.PermissionGranted, LocalizationStore.Current.FailedGrantPermission);

	private Task RevokePermissionAsync() =>
		GrantOrRevokeAsync((roleName, permType, keyPrefix) => _etcdClient.RevokePermissionAsync(roleName, permType, keyPrefix), LocalizationStore.Current.PermissionRevoked, LocalizationStore.Current.FailedRevokePermission);

	private async Task GrantOrRevokeAsync(Func<string, PermissionType, string, Task> action, string successMessage, string failureMessage)
	{
		var roleName = Prompt.Ask(LocalizationStore.Current.EnterRoleNamePrompt);

		if (roleName is null)
			return;

		var keyPrefix = Prompt.Ask(LocalizationStore.Current.EnterKeyPrefix);

		if (keyPrefix is null)
			return;

		var permType = PermissionTypeSelector.Select();

		if (permType is null)
			return;

		try
		{
			await action(roleName, permType.Value, keyPrefix);
			AnsiConsole.MarkupLine(successMessage);
		}
		catch
		{
			AnsiConsole.MarkupLine(failureMessage);
		}

		PressAnyKeyPrompt.Show();
	}
}