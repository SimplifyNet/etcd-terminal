using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens;
using EtcdTerminal.Localization;
using EtcdTerminal.Security;
using EtcdTerminal.Terminal;
using EtcdTerminal.Permissions;
using EtcdTerminal.Roles;

namespace EtcdTerminal.App.Screens.Roles;

public sealed class RoleManagementScreen(ITerminal _terminal, IEtcdRoleAdmin _roleAdmin, MenuScreen _menuScreen, PermissionTypeSelector _permissionTypeSelector, PermissionScopeSelector _permissionScopeSelector, PressAnyKeyPrompt _pressAnyKey, Prompt _prompt, 	Spinner _spinner, Message _message, ILocalization _localization) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.ManageRoles;

	public string Label => _localization.ManageRoles;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanManageAuth;
	public async Task ShowAsync() =>
		await _menuScreen.RunAsync<RoleMenuAction>(_localization.RoleManagement,
		[
			new(RoleMenuAction.ListRoles, _localization.ListRoles),
			new(RoleMenuAction.CreateRole, _localization.CreateRole),
			new(RoleMenuAction.DeleteRole, _localization.DeleteRole),
			new(RoleMenuAction.GrantPermission, _localization.GrantPermission),
			new(RoleMenuAction.RevokePermission, _localization.RevokePermission)
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

		var loaded = await _spinner.RunAsync(_localization.LoadingRoles, async ct =>
		{
			roles = await _roleAdmin.GetRolesAsync(ct);
		});

		if (!loaded)
		{
			_message.ShowWarning(_localization.OperationCancelled);

			return;
		}

		RoleListRenderer.Render(_terminal, _localization, roles);

		_terminal.WriteLine();
		_pressAnyKey.Show();
	}

	private async Task CreateRoleAsync()
	{
		var roleName = _prompt.Ask(_localization.EnterRoleNamePrompt);

		if (roleName is null)
			return;

		var result = await _roleAdmin.CreateRoleAsync(roleName);

		_message.ShowResult(result.Success, _localization.RoleCreated, result.ErrorMessage ?? _localization.FailedCreateRole);
	}

	private async Task DeleteRoleAsync()
	{
		var roleName = _prompt.Ask(_localization.EnterRoleNameToDelete);

		if (roleName is null)
			return;

		var result = await _roleAdmin.DeleteRoleAsync(roleName);

		_message.ShowResult(result.Success, _localization.RoleDeleted, result.ErrorMessage ?? _localization.FailedDeleteRole);
	}

	private Task GrantPermissionAsync() =>
		GrantOrRevokeAsync((roleName, permType, key, scope) => _roleAdmin.GrantPermissionAsync(roleName, permType, key, scope), _localization.PermissionGranted, _localization.FailedGrantPermission);

	private Task RevokePermissionAsync() =>
		GrantOrRevokeAsync((roleName, permType, key, scope) => _roleAdmin.RevokePermissionAsync(roleName, permType, key, scope), _localization.PermissionRevoked, _localization.FailedRevokePermission);

	private async Task GrantOrRevokeAsync(Func<string, PermissionType, string, PermissionScope, Task<EtcdOperationResult>> action, string successMessage, string failureMessage)
	{
		var roleName = _prompt.Ask(_localization.EnterRoleNamePrompt);

		if (roleName is null)
			return;

		var scope = _permissionScopeSelector.Select();

		if (scope is null)
			return;

		var keyPromptText = scope is PermissionScope.Prefix
			? _localization.EnterKeyPrefix
			: _localization.EnterExactKey;

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
