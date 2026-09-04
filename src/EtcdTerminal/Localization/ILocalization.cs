namespace EtcdTerminal.Localization;

public interface ILocalization
{
	string Name { get; }

	string InstanceAdded { get; }
	string InstanceRemoved { get; }
	string InstanceUpdated { get; }
	string ConnectedSuccess { get; }
	string InvalidConnStr { get; }
	string ManageConnections { get; }
	string AddInstance { get; }
	string EditInstance { get; }
	string MoveUpInstance { get; }
	string MoveDownInstance { get; }
	string RemoveInstance { get; }
	string Exit { get; }
	string Settings { get; }
	string NoConnectionsMessage { get; }
	string SelectInstanceToEdit { get; }
	string SelectInstanceToMoveUp { get; }
	string SelectInstanceToMoveDown { get; }
	string SelectInstanceToRemove { get; }
	string EnterInstanceName { get; }
	string EnterConnStr { get; }
	string DefaultConnStr { get; }
	string EnterUsername { get; }
	string EnterPassword { get; }
	string Connecting { get; }
	string ChangePassword { get; }
	string AreYouSureRemove { get; }

	string BrowseKeys { get; }
	string CreateKey { get; }
	string ManageUsers { get; }
	string ManageRoles { get; }
	string ViewPermissions { get; }
	string Disconnect { get; }

	string SettingsTitle { get; }
	string PageSizeItem { get; }
	string TrimInputValuesItem { get; }
	string PageSizeLabel { get; }
	string TrimInputValuesLabel { get; }
	string EnterPageSize { get; }
	string InvalidPageSize { get; }
	string SettingsSaved { get; }
	string On { get; }
	string Off { get; }

	string EnterNewValue { get; }
	string AreYouSure { get; }
	string EditingKey { get; }
	string CurrentValue { get; }
	string DeleteKey { get; }
	string KeyUpdated { get; }
	string CouldNotUpdateKey { get; }
	string KeyDeleted { get; }
	string KeyCouldNotBeDeleted { get; }
	string NoKeysFound { get; }
	string Page { get; }
	string TotalKeys { get; }
	string Selected { get; }
	string Edit { get; }
	string Delete { get; }
	string Cancel { get; }
	string TypeToSearch { get; }

	string EnterKey { get; }
	string EnterValue { get; }
	string KeyCreated { get; }
	string KeyCreateFailed { get; }

	string UserManagement { get; }
	string ListUsers { get; }
	string CreateUser { get; }
	string DeleteUser { get; }
	string AssignRole { get; }
	string RemoveRole { get; }
	string EnterUsernamePrompt { get; }
	string EnterPasswordPrompt { get; }
	string UserCreated { get; }
	string FailedCreateUser { get; }
	string EnterUsernameToDelete { get; }
	string DeleteUserConfirm { get; }
	string UserDeleted { get; }
	string FailedDeleteUser { get; }
	string EnterNewPassword { get; }
	string PasswordChanged { get; }
	string FailedChangePassword { get; }
	string EnterRoleName { get; }
	string RoleAssigned { get; }
	string FailedAssignRole { get; }
	string EnterRoleNameToRemove { get; }
	string RoleRemoved { get; }
	string FailedRemoveRole { get; }

	string RoleManagement { get; }
	string ListRoles { get; }
	string CreateRole { get; }
	string DeleteRole { get; }
	string GrantPermission { get; }
	string RevokePermission { get; }
	string EnterRoleNamePrompt { get; }
	string RoleCreated { get; }
	string FailedCreateRole { get; }
	string EnterRoleNameToDelete { get; }
	string DeleteRoleConfirm { get; }
	string RoleDeleted { get; }
	string FailedDeleteRole { get; }
	string EnterKeyPrefix { get; }
	string PermissionGranted { get; }
	string FailedGrantPermission { get; }
	string PermissionRevoked { get; }
	string FailedRevokePermission { get; }
	string SelectPermissionType { get; }
	string Read { get; }
	string Write { get; }
	string ReadWrite { get; }

	string LoadingPermissions { get; }
	string NoUsersOrRoles { get; }
	string NoRolesFound { get; }
	string NoUsersFound { get; }
	string Username { get; }
	string Roles { get; }
	string Role { get; }
	string Permissions { get; }
	string PermissionType { get; }
	string KeyPrefix { get; }
	string None { get; }
	string NoRoles { get; }
	string NoPermissions { get; }
	string PressAnyKey { get; }
	string PressAnyKeyRestart { get; }
}