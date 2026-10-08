using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Security;
using EtcdTerminal.Users;

namespace EtcdTerminal.App.Screens.Users;

public sealed class UsersManagementScreen(IEtcdUserAdmin _userAdmin, MenuScreen _menuScreen, UserListLayout _layout, ListBrowser _browser, UserInput _input, Spinner _spinner, Message _message, ILocalization _localization) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.ManageUsers;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanManageAuth;
	public async Task ShowAsync() =>
		await _menuScreen.RunAsync<UserMenuAction>(_localization.UsersManagement,
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
		var username = _input.Ask(_localization.EnterUsernamePrompt);

		if (username is null)
			return;

		var password = _input.Secret(_localization.EnterPasswordPrompt);

		if (password is null)
			return;

		var result = await _userAdmin.CreateUserAsync(username, password);

		_message.ShowResult(result, _localization.UserCreated, _localization.FailedCreateUser);
	}

	private async Task DeleteUserAsync()
	{
		var username = _input.Ask(_localization.EnterUsernameToDelete);

		if (username is null)
			return;

		var result = await _userAdmin.DeleteUserAsync(username);

		_message.ShowResult(result, _localization.UserDeleted, _localization.FailedDeleteUser);
	}

	private async Task ChangePasswordAsync()
	{
		var username = _input.Ask(_localization.EnterUsernamePrompt);

		if (username is null)
			return;

		var newPassword = _input.Secret(_localization.EnterNewPassword);

		if (newPassword is null)
			return;

		var result = await _userAdmin.ChangeUserPasswordAsync(username, newPassword);

		_message.ShowResult(result, _localization.PasswordChanged, _localization.FailedChangePassword);
	}

	private async Task AssignRoleAsync()
	{
		var username = _input.Ask(_localization.EnterUsernamePrompt);

		if (username is null)
			return;

		var roleName = _input.Ask(_localization.EnterRoleName);

		if (roleName is null)
			return;

		var grantResult = await _userAdmin.GrantRoleToUserAsync(username, roleName);

		_message.ShowResult(grantResult, _localization.RoleAssigned, _localization.FailedAssignRole);
	}

	private async Task RevokeRoleAsync()
	{
		var username = _input.Ask(_localization.EnterUsernamePrompt);

		if (username is null)
			return;

		var roleName = _input.Ask(_localization.EnterRoleNameToRemove);

		if (roleName is null)
			return;

		var revokeResult = await _userAdmin.RevokeRoleFromUserAsync(username, roleName);

		_message.ShowResult(revokeResult, _localization.RoleRemoved, _localization.FailedRemoveRole);
	}
}
