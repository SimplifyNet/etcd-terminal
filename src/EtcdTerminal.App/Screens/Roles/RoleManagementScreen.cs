using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using Spectre.Console;
using EtcdTerminal.Permissions;

namespace EtcdTerminal.App.Screens.Roles;

public sealed class RoleManagementScreen(IEtcdClient _etcdClient, MenuScreen _menuScreen, PermissionTypeSelector _permissionTypeSelector, PressAnyKeyPrompt _pressAnyKey)
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

		RoleListRenderer.Render(roles);

		_pressAnyKey.Show();
	}

	private async Task CreateRoleAsync()
	{
		var roleName = Prompt.Ask(LocalizationStore.Current.EnterRoleNamePrompt);

		if (roleName is null)
			return;

		var result = await _etcdClient.CreateRoleAsync(roleName);

		if (result)
			AnsiConsole.MarkupLine($"[green]{LocalizationStore.Current.RoleCreated}[/]");
		else
			AnsiConsole.MarkupLine($"[red]{LocalizationStore.Current.FailedCreateRole}[/]");

		_pressAnyKey.Show();
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
			AnsiConsole.MarkupLine($"[green]{LocalizationStore.Current.RoleDeleted}[/]");
		else
			AnsiConsole.MarkupLine($"[red]{LocalizationStore.Current.FailedDeleteRole}[/]");

		_pressAnyKey.Show();
	}

	private Task GrantPermissionAsync() =>
		GrantOrRevokeAsync((roleName, permType, keyPrefix) => _etcdClient.GrantPermissionAsync(roleName, permType, keyPrefix), $"[green]{LocalizationStore.Current.PermissionGranted}[/]", $"[red]{LocalizationStore.Current.FailedGrantPermission}[/]");

	private Task RevokePermissionAsync() =>
		GrantOrRevokeAsync((roleName, permType, keyPrefix) => _etcdClient.RevokePermissionAsync(roleName, permType, keyPrefix), $"[green]{LocalizationStore.Current.PermissionRevoked}[/]", $"[red]{LocalizationStore.Current.FailedRevokePermission}[/]");

	private async Task GrantOrRevokeAsync(Func<string, PermissionType, string, Task> action, string successMessage, string failureMessage)
	{
		var roleName = Prompt.Ask(LocalizationStore.Current.EnterRoleNamePrompt);

		if (roleName is null)
			return;

		var keyPrefix = Prompt.Ask(LocalizationStore.Current.EnterKeyPrefix);

		if (keyPrefix is null)
			return;

		var permType = _permissionTypeSelector.Select();

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

		_pressAnyKey.Show();
	}
}
