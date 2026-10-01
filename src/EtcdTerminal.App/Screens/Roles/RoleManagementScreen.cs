using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Security;
using EtcdTerminal.Configuration;
using EtcdTerminal.Permissions;
using EtcdTerminal.Roles;

namespace EtcdTerminal.App.Screens.Roles;

public sealed class RoleManagementScreen(IEtcdRoleAdmin _roleAdmin, MenuScreen _menuScreen, PermissionTypeSelector _permissionTypeSelector, PermissionScopeSelector _permissionScopeSelector, PressAnyKeyPrompt _pressAnyKey, RoleListLayout _layout, Prompt _prompt, Spinner _spinner, Message _message, ILocalization _localization, IAppSettingsStore _settings) : IMainMenuEntry
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

		_pressAnyKey.Show(_layout.Body(roles));
	}

	private async Task CreateRoleAsync()
	{
		var trim = _settings.Current.TrimInputValues;

		var roleName = _prompt.Ask(_localization.EnterRoleNamePrompt, trim: trim);

		if (roleName is null)
			return;

		var result = await _roleAdmin.CreateRoleAsync(roleName);

		_message.ShowResult(result.Success, _localization.RoleCreated, result.ErrorMessage ?? _localization.FailedCreateRole);
	}

	private async Task DeleteRoleAsync()
	{
		var trim = _settings.Current.TrimInputValues;

		var roleName = _prompt.Ask(_localization.EnterRoleNameToDelete, trim: trim);

		if (roleName is null)
			return;

		var result = await _roleAdmin.DeleteRoleAsync(roleName);

		_message.ShowResult(result.Success, _localization.RoleDeleted, result.ErrorMessage ?? _localization.FailedDeleteRole);
	}

	private async Task GrantPermissionAsync()
	{
		var target = PromptTarget();

		if (target is null)
			return;

		var permType = _permissionTypeSelector.Select();

		if (permType is null)
			return;

		var result = await _roleAdmin.GrantPermissionAsync(target.RoleName, permType.Value, target.Key, target.Scope);

		_message.ShowResult(result.Success, _localization.PermissionGranted, result.ErrorMessage ?? _localization.FailedGrantPermission);
	}

	private async Task RevokePermissionAsync()
	{
		var target = PromptTarget();

		if (target is null)
			return;

		// Revocation removes the whole permission for the target interval,
		// so no permission type is requested here.
		var result = await _roleAdmin.RevokePermissionAsync(target.RoleName, target.Key, target.Scope);

		_message.ShowResult(result.Success, _localization.PermissionRevoked, result.ErrorMessage ?? _localization.FailedRevokePermission);
	}

	private PermissionTarget? PromptTarget()
	{
		var trim = _settings.Current.TrimInputValues;

		var roleName = _prompt.Ask(_localization.EnterRoleNamePrompt, trim: trim);

		if (roleName is null)
			return null;

		var scope = _permissionScopeSelector.Select();

		if (scope is null)
			return null;

		var keyPromptText = scope is PermissionScope.Prefix
			? _localization.EnterKeyPrefix
			: _localization.EnterExactKey;

		var key = _prompt.Ask(keyPromptText, trim: trim);

		if (key is null)
			return null;

		return new(roleName, key, scope.Value);
	}

	private sealed record PermissionTarget(string RoleName, string Key, PermissionScope Scope);
}
