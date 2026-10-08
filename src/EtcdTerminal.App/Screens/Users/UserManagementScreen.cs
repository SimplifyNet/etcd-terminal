using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Configuration;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Security;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Users;

public sealed class UserManagementScreen(IEtcdUserAdmin _userAdmin, MenuScreen _menuScreen, UserListLayout _layout, ListBrowser _browser, Prompt _prompt, Spinner _spinner, Message _message, ILocalization _localization, IAppSettingsStore _settings) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.ManageUsers;

	public string Label => _localization.ManageUsers;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanManageAuth;
	public async Task ShowAsync() =>
		await _menuScreen.RunAsync<UserMenuAction>(_localization.UserManagement,
		[
			new(UserMenuAction.ListUsers, _localization.ListUsers),
			new(UserMenuAction.CreateUser, _localization.CreateUser),
			new(UserMenuAction.DeleteUser, _localization.DeleteUser),
			new(UserMenuAction.ChangePassword, _localization.ChangePassword),
			new(UserMenuAction.AssignRole, _localization.AssignRole),
			new(UserMenuAction.RemoveRole, _localization.RemoveRole)
		],
		HandleChoiceAsync);

	private async Task HandleChoiceAsync(UserMenuAction action)
	{
		switch (action)
		{
			case UserMenuAction.ListUsers:
				await ListUsersAsync();
				break;
			case UserMenuAction.CreateUser:
				await CreateUserAsync();
				break;
			case UserMenuAction.DeleteUser:
				await DeleteUserAsync();
				break;
			case UserMenuAction.ChangePassword:
				await ChangePasswordAsync();
				break;
			case UserMenuAction.AssignRole:
				await AssignRoleAsync();
				break;
			case UserMenuAction.RemoveRole:
				await RevokeRoleAsync();
				break;
		}
	}

	/// The view-only page: the list, filterable and paginated like the key
	/// browser instead of a single framed screen.
	private async Task ListUsersAsync()
	{
		IReadOnlyList<EtcdUser> users = [];

		var loaded = await _spinner.RunAsync(_localization.LoadingUsers, async ct =>
		{
			users = await _userAdmin.GetUsersAsync(ct);
		});

		if (!loaded)
		{
			_message.ShowWarning(_localization.OperationCancelled);

			return;
		}

		_browser.Show(
			_layout.Headers(),
			_layout.Rows(users),
			_localization.NoUsersFound,
			_localization.TotalUsers);
	}

	private async Task CreateUserAsync()
	{
		var trim = _settings.Current.TrimInputValues;

		var username = _prompt.Ask(_localization.EnterUsernamePrompt, trim: trim);

		if (username is null)
			return;

		var password = _prompt.Secret(_localization.EnterPasswordPrompt, trim: trim);

		if (password is null)
			return;

		var result = await _userAdmin.CreateUserAsync(username, password);

		_message.ShowResult(result.Success, _localization.UserCreated, _localization.FailedCreateUser + "\n" + result.ErrorMessage);
	}

	private async Task DeleteUserAsync()
	{
		var trim = _settings.Current.TrimInputValues;

		var username = _prompt.Ask(_localization.EnterUsernameToDelete, trim: trim);

		if (username is null)
			return;

		var result = await _userAdmin.DeleteUserAsync(username);

		_message.ShowResult(result.Success, _localization.UserDeleted, _localization.FailedDeleteUser + "\n" + result.ErrorMessage);
	}

	private async Task ChangePasswordAsync()
	{
		var trim = _settings.Current.TrimInputValues;

		var username = _prompt.Ask(_localization.EnterUsernamePrompt, trim: trim);

		if (username is null)
			return;

		var newPassword = _prompt.Secret(_localization.EnterNewPassword, trim: trim);

		if (newPassword is null)
			return;

		var result = await _userAdmin.ChangeUserPasswordAsync(username, newPassword);

		_message.ShowResult(result.Success, _localization.PasswordChanged, _localization.FailedChangePassword + "\n" + result.ErrorMessage);
	}

	private async Task AssignRoleAsync()
	{
		var trim = _settings.Current.TrimInputValues;

		var username = _prompt.Ask(_localization.EnterUsernamePrompt, trim: trim);

		if (username is null)
			return;

		var roleName = _prompt.Ask(_localization.EnterRoleName, trim: trim);

		if (roleName is null)
			return;

		var grantResult = await _userAdmin.GrantRoleToUserAsync(username, roleName);

		_message.ShowResult(grantResult.Success, _localization.RoleAssigned, _localization.FailedAssignRole + "\n" + grantResult.ErrorMessage);
	}

	private async Task RevokeRoleAsync()
	{
		var trim = _settings.Current.TrimInputValues;

		var username = _prompt.Ask(_localization.EnterUsernamePrompt, trim: trim);

		if (username is null)
			return;

		var roleName = _prompt.Ask(_localization.EnterRoleNameToRemove, trim: trim);

		if (roleName is null)
			return;

		var revokeResult = await _userAdmin.RevokeRoleFromUserAsync(username, roleName);

		_message.ShowResult(revokeResult.Success, _localization.RoleRemoved, _localization.FailedRemoveRole + "\n" + revokeResult.ErrorMessage);
	}
}
