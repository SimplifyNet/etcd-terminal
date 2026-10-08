using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Security;
using EtcdTerminal.Configuration;
using EtcdTerminal.Permissions;
using EtcdTerminal.Roles;

namespace EtcdTerminal.App.Screens.Roles;

public sealed class RoleManagementScreen(IEtcdRoleAdmin _roleAdmin, MenuScreen _menuScreen, PermissionTypeSelector _permissionTypeSelector, PermissionScopeSelector _permissionScopeSelector, RoleListLayout _layout, ListBrowser _browser, Prompt _prompt, Spinner _spinner, Message _message, ILocalization _localization, IAppSettingsStore _settings) : IMainMenuEntry
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

	/// The view-only page: one row per permission, filterable and paginated
	/// like the key browser instead of a sectioned framed screen.
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

		// A row of this page is one permission of a role, so the pagination
		// counts the permission rows it filters; "total roles" would promise
		// a count the rows do not hold.
		_browser.Show(
			_layout.Headers(),
			_layout.Rows(roles),
			_localization.NoRolesFound,
			_localization.TotalPermissions);
	}

	private async Task CreateRoleAsync()
	{
		var trim = _settings.Current.TrimInputValues;

		var roleName = _prompt.Ask(_localization.EnterRoleNamePrompt, trim: trim);

		if (roleName is null)
			return;

		var result = await _roleAdmin.CreateRoleAsync(roleName);

		_message.ShowResult(result.Success, _localization.RoleCreated, _localization.FailedCreateRole + "\n" + result.ErrorMessage);
	}

	private async Task DeleteRoleAsync()
	{
		var trim = _settings.Current.TrimInputValues;

		var roleName = _prompt.Ask(_localization.EnterRoleNameToDelete, trim: trim);

		if (roleName is null)
			return;

		var result = await _roleAdmin.DeleteRoleAsync(roleName);

		_message.ShowResult(result.Success, _localization.RoleDeleted, _localization.FailedDeleteRole + "\n" + result.ErrorMessage);
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

		_message.ShowResult(result.Success, _localization.PermissionGranted, _localization.FailedGrantPermission + "\n" + result.ErrorMessage);
	}

	private async Task RevokePermissionAsync()
	{
		var target = PromptTarget();

		if (target is null)
			return;

		// Revocation removes the whole permission for the target interval,
		// so no permission type is requested here.
		var result = await _roleAdmin.RevokePermissionAsync(target.RoleName, target.Key, target.Scope);

		_message.ShowResult(result.Success, _localization.PermissionRevoked, _localization.FailedRevokePermission + "\n" + result.ErrorMessage);
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
