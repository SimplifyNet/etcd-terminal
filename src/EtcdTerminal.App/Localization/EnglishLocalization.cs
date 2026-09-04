using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Localization;

public class EnglishLocalization : ILocalization
{
	public string Name => "English";

	public string InstanceAdded => "Instance added successfully!";
	public string InstanceRemoved => "Instance removed successfully!";
	public string InstanceUpdated => "Instance updated successfully!";
	public string ConnectedSuccess => "Connected successfully!";
	public string InvalidConnStr => "Invalid connection string. Must be a valid http or https URL.";
	public string ManageConnections => "Manage Connections";
	public string AddInstance => "Add Instance";
	public string EditInstance => "Edit Instance";
	public string MoveUpInstance => "Move Up";
	public string MoveDownInstance => "Move Down";
	public string RemoveInstance => "Remove Instance";
	public string Exit => "Exit";
	public string Settings => "Settings";
	public string NoConnectionsMessage => "No connections configured. Go to Manage Connections to add one.";
	public string SelectInstanceToEdit => "Select instance to edit:";
	public string SelectInstanceToMoveUp => "Select instance to move up:";
	public string SelectInstanceToMoveDown => "Select instance to move down:";
	public string SelectInstanceToRemove => "Select instance to remove:";
	public string EnterInstanceName => "Enter instance name:";
	public string EnterConnStr => "Enter connection string:";
	public string DefaultConnStr => "http://localhost:2379";
	public string EnterUsername => "Enter username (optional, leave empty for none):";
	public string EnterPassword => "Enter password:";
	public string Connecting => "Connecting...";
	public string ChangePassword => "Change password?";
	public string AreYouSureRemove => "Are you sure you want to remove {0}?";

	public string BrowseKeys => "Browse Keys";
	public string CreateKey => "Create Key";
	public string ManageUsers => "Manage Users";
	public string ManageRoles => "Manage Roles";
	public string ViewPermissions => "View Permissions";
	public string Disconnect => "Disconnect";

	public string SettingsTitle => "Settings";
	public string PageSizeItem => "PageSize";
	public string TrimInputValuesItem => "TrimInputValues";
	public string PageSizeLabel => "Keys per page";
	public string TrimInputValuesLabel => "Trim input values";
	public string EnterPageSize => "Enter keys per page (1-500):";
	public string InvalidPageSize => "Invalid page size. Must be a number from 1 to 500.";
	public string SettingsSaved => "Settings saved!";
	public string On => "On";
	public string Off => "Off";

	public string EnterNewValue => "Enter new value:";
	public string AreYouSure => "Are you sure?";
	public string EditingKey => "Editing key:";
	public string CurrentValue => "Current value:";
	public string DeleteKey => "Delete key:";
	public string KeyUpdated => "Key updated successfully!";
	public string CouldNotUpdateKey => "Could not update key.";
	public string KeyDeleted => "Key deleted successfully!";
	public string KeyCouldNotBeDeleted => "Key could not be deleted.";
	public string NoKeysFound => "  No keys found.";
	public string Page => "Page";
	public string TotalKeys => "total keys";
	public string Selected => "Selected:";
	public string Edit => "Edit";
	public string Delete => "Delete";
	public string Cancel => "Cancel";
	public string TypeToSearch => "  \U0001f50d  Type to search...";

	public string EnterKey => "Enter key:";
	public string EnterValue => "Enter value:";
	public string KeyCreated => "Key created successfully!";
	public string KeyCreateFailed => "Key already exists or could not be created.";

	public string UserManagement => "User Management";
	public string ListUsers => "List Users";
	public string CreateUser => "Create User";
	public string DeleteUser => "Delete User";
	public string AssignRole => "Assign Role to User";
	public string RemoveRole => "Remove Role from User";
	public string EnterUsernamePrompt => "Enter username:";
	public string EnterPasswordPrompt => "Enter password:";
	public string UserCreated => "User created successfully!";
	public string FailedCreateUser => "Failed to create user.";
	public string EnterUsernameToDelete => "Enter username to delete:";
	public string DeleteUserConfirm => "Are you sure you want to delete user {0}?";
	public string UserDeleted => "User deleted successfully!";
	public string FailedDeleteUser => "Failed to delete user.";
	public string EnterNewPassword => "Enter new password:";
	public string PasswordChanged => "Password changed successfully!";
	public string FailedChangePassword => "Failed to change password.";
	public string EnterRoleName => "Enter role name:";
	public string RoleAssigned => "Role assigned successfully!";
	public string FailedAssignRole => "Failed to assign role.";
	public string EnterRoleNameToRemove => "Enter role name to remove:";
	public string RoleRemoved => "Role removed successfully!";
	public string FailedRemoveRole => "Failed to remove role.";

	public string RoleManagement => "Role Management";
	public string ListRoles => "List Roles";
	public string CreateRole => "Create Role";
	public string DeleteRole => "Delete Role";
	public string GrantPermission => "Grant Permission";
	public string RevokePermission => "Revoke Permission";
	public string EnterRoleNamePrompt => "Enter role name:";
	public string RoleCreated => "Role created successfully!";
	public string FailedCreateRole => "Failed to create role (may already exist).";
	public string EnterRoleNameToDelete => "Enter role name to delete:";
	public string DeleteRoleConfirm => "Are you sure you want to delete role {0}?";
	public string RoleDeleted => "Role deleted successfully!";
	public string FailedDeleteRole => "Failed to delete role.";
	public string EnterKeyPrefix => "Enter key prefix:";
	public string PermissionGranted => "Permission granted successfully!";
	public string FailedGrantPermission => "Failed to grant permission.";
	public string PermissionRevoked => "Permission revoked successfully!";
	public string FailedRevokePermission => "Failed to revoke permission.";
	public string SelectPermissionType => "Select permission type:";
	public string Read => "Read";
	public string Write => "Write";
	public string ReadWrite => "ReadWrite";

	public string LoadingPermissions => "Loading permissions...";
	public string NoUsersOrRoles => "No users or roles found.";
	public string NoRolesFound => "No roles found.";
	public string NoUsersFound => "No users found.";
	public string Username => "Username";
	public string Roles => "Roles";
	public string Role => "Role";
	public string Permissions => "Permissions";
	public string PermissionType => "Permission Type";
	public string KeyPrefix => "Key Prefix";
	public string None => "none";
	public string NoRoles => "no roles";
	public string NoPermissions => "no permissions";
	public string PressAnyKey => "Press any key to continue...";
	public string PressAnyKeyRestart => "\nPress any key to restart...";
}
