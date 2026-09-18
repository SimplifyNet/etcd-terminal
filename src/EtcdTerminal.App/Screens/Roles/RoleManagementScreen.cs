using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens;
using EtcdTerminal.Localization;
using EtcdTerminal.Security;
using EtcdTerminal.Terminal;
using EtcdTerminal.Permissions;
using EtcdTerminal.Roles;

namespace EtcdTerminal.App.Screens.Roles;

public sealed class RoleManagementScreen(ITerminal _terminal, IEtcdRoleAdmin _roleAdmin, MenuScreen _menuScreen, PermissionTypeSelector _permissionTypeSelector, PermissionScopeSelector _permissionScopeSelector, PressAnyKeyPrompt _pressAnyKey, Prompt _prompt, Spinner _spinner, Message _message) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.ManageRoles;

	public string Label => LocalizationStore.Current.ManageRoles;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanManageAuth;
	public async Task ShowAsync() =>
		await _menuScreen.RunAsync<RoleMenuAction>(LocalizationStore.Current.RoleManagement,
		[
			new(RoleMenuAction.ListRoles, LocalizationStore.Current.ListRoles),
			new(RoleMenuAction.CreateRole, LocalizationStore.Current.CreateRole),
			new(RoleMenuAction.DeleteRole, LocalizationStore.Current.DeleteRole),
			new(RoleMenuAction.GrantPermission, LocalizationStore.Current.GrantPermission),
			new(RoleMenuAction.RevokePermission, LocalizationStore.Current.RevokePermission)
		],
		HandleChoiceAsync);

	private async Task HandleChoiceAsync(RoleMenuAction action)
	{
		switch (action)
		{
			case RoleMenuAction.ListRoles:
				await ListRolesAsync();
				break;
			case RoleMenuAction.CreateRole:
				await CreateRoleAsync();
				break;
			case RoleMenuAction.DeleteRole:
				await DeleteRoleAsync();
				break;
			case RoleMenuAction.GrantPermission:
				await GrantPermissionAsync();
				break;
			case RoleMenuAction.RevokePermission:
				await RevokePermissionAsync();
				break;
		}
	}

	private async Task ListRolesAsync()
	{
		IReadOnlyList<EtcdRole> roles = [];

		var loaded = await _spinner.RunAsync(LocalizationStore.Current.LoadingRoles, async ct =>
		{
			roles = await _roleAdmin.GetRolesAsync(ct);
		});

		if (!loaded)
		{
			_message.ShowWarning(LocalizationStore.Current.OperationCancelled);

			return;
		}

		RoleListRenderer.Render(_terminal, roles);

		_terminal.WriteLine();
		_pressAnyKey.Show();
	}

	private async Task CreateRoleAsync()
	{
		var roleName = _prompt.Ask(LocalizationStore.Current.EnterRoleNamePrompt);

		if (roleName is null)
			return;

		var result = await _roleAdmin.CreateRoleAsync(roleName);

		_message.ShowResult(result.Success, LocalizationStore.Current.RoleCreated, result.ErrorMessage ?? LocalizationStore.Current.FailedCreateRole);
	}

	private async Task DeleteRoleAsync()
	{
		var roleName = _prompt.Ask(LocalizationStore.Current.EnterRoleNameToDelete);

		if (roleName is null)
			return;

		var result = await _roleAdmin.DeleteRoleAsync(roleName);

		_message.ShowResult(result.Success, LocalizationStore.Current.RoleDeleted, result.ErrorMessage ?? LocalizationStore.Current.FailedDeleteRole);
	}

	private Task GrantPermissionAsync() =>
		GrantOrRevokeAsync((roleName, permType, key, scope) => _roleAdmin.GrantPermissionAsync(roleName, permType, key, scope), LocalizationStore.Current.PermissionGranted, LocalizationStore.Current.FailedGrantPermission);

	private Task RevokePermissionAsync() =>
		GrantOrRevokeAsync((roleName, permType, key, scope) => _roleAdmin.RevokePermissionAsync(roleName, permType, key, scope), LocalizationStore.Current.PermissionRevoked, LocalizationStore.Current.FailedRevokePermission);

	private async Task GrantOrRevokeAsync(Func<string, PermissionType, string, PermissionScope, Task<EtcdOperationResult>> action, string successMessage, string failureMessage)
	{
		var roleName = _prompt.Ask(LocalizationStore.Current.EnterRoleNamePrompt);

		if (roleName is null)
			return;

		var scope = _permissionScopeSelector.Select();

		if (scope is null)
			return;

		var keyPromptText = scope is PermissionScope.Prefix
			? LocalizationStore.Current.EnterKeyPrefix
			: LocalizationStore.Current.EnterExactKey;

		var key = _prompt.Ask(keyPromptText);

		if (key is null)
			return;

		var permType = _permissionTypeSelector.Select();

		if (permType is null)
			return;

		var result = await action(roleName, permType.Value, key, scope.Value);

		_message.ShowResult(result.Success, successMessage, result.ErrorMessage ?? failureMessage);
	}
}
