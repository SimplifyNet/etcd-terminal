using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Localization;

public class EnglishLocalization : ILocalization
{
	public string Name => "English";

	public string InstanceAdded => "[green]Instance added successfully![/]";
	public string InstanceRemoved => "[green]Instance removed successfully![/]";
	public string InstanceUpdated => "[green]Instance updated successfully![/]";
	public string ConnectedSuccess => "[green]Connected successfully![/]";
	public string InvalidConnStr => "[red]Invalid connection string. Must be a valid http or https URL.[/]";
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
	public string InvalidPageSize => "[red]Invalid page size. Must be a number from 1 to 500.[/]";
	public string SettingsSaved => "[green]Settings saved![/]";
	public string On => "On";
	public string Off => "Off";

	public string EnterNewValue => "Enter new value:";
	public string AreYouSure => "Are you sure?";
	public string EditingKey => "Editing key:";
	public string CurrentValue => "Current value:";
	public string DeleteKey => "Delete key:";
	public string KeyUpdated => "[green]Key updated successfully![/]";
	public string CouldNotUpdateKey => "[red]Could not update key.[/]";
	public string KeyDeleted => "[green]Key deleted successfully![/]";
	public string KeyCouldNotBeDeleted => "[red]Key could not be deleted.[/]";
	public string NoKeysFound => "  [grey]No keys found.[/]";
	public string Page => "Page";
	public string TotalKeys => "total keys";
	public string Selected => "Selected:";
	public string Edit => "Edit";
	public string Delete => "Delete";
	public string Cancel => "Cancel";
	public string TypeToSearch => "  \U0001f50d  Type to search...";

	public string EnterKey => "Enter key:";
	public string EnterValue => "Enter value:";
	public string KeyCreated => "[green]Key created successfully![/]";
	public string KeyCreateFailed => "[red]Key already exists or could not be created.[/]";

	public string UserManagement => "User Management";
	public string ListUsers => "List Users";
	public string CreateUser => "Create User";
	public string DeleteUser => "Delete User";
	public string AssignRole => "Assign Role to User";
	public string RemoveRole => "Remove Role from User";
	public string EnterUsernamePrompt => "Enter username:";
	public string EnterPasswordPrompt => "Enter password:";
	public string UserCreated => "[green]User created successfully![/]";
	public string FailedCreateUser => "[red]Failed to create user.[/]";
	public string EnterUsernameToDelete => "Enter username to delete:";
	public string DeleteUserConfirm => "Are you sure you want to delete user {0}?";
	public string UserDeleted => "[green]User deleted successfully![/]";
	public string FailedDeleteUser => "[red]Failed to delete user.[/]";
	public string EnterNewPassword => "Enter new password:";
	public string PasswordChanged => "[green]Password changed successfully![/]";
	public string FailedChangePassword => "[red]Failed to change password.[/]";
	public string EnterRoleName => "Enter role name:";
	public string RoleAssigned => "[green]Role assigned successfully![/]";
	public string FailedAssignRole => "[red]Failed to assign role.[/]";
	public string EnterRoleNameToRemove => "Enter role name to remove:";
	public string RoleRemoved => "[green]Role removed successfully![/]";
	public string FailedRemoveRole => "[red]Failed to remove role.[/]";

	public string RoleManagement => "Role Management";
	public string ListRoles => "List Roles";
	public string CreateRole => "Create Role";
	public string DeleteRole => "Delete Role";
	public string GrantPermission => "Grant Permission";
	public string RevokePermission => "Revoke Permission";
	public string EnterRoleNamePrompt => "Enter role name:";
	public string RoleCreated => "[green]Role created successfully![/]";
	public string FailedCreateRole => "[red]Failed to create role (may already exist).[/]";
	public string EnterRoleNameToDelete => "Enter role name to delete:";
	public string DeleteRoleConfirm => "Are you sure you want to delete role {0}?";
	public string RoleDeleted => "[green]Role deleted successfully![/]";
	public string FailedDeleteRole => "[red]Failed to delete role.[/]";
	public string EnterKeyPrefix => "Enter key prefix:";
	public string PermissionGranted => "[green]Permission granted successfully![/]";
	public string FailedGrantPermission => "[red]Failed to grant permission.[/]";
	public string PermissionRevoked => "[green]Permission revoked successfully![/]";
	public string FailedRevokePermission => "[red]Failed to revoke permission.[/]";
	public string SelectPermissionType => "Select permission type:";
	public string Read => "Read";
	public string Write => "Write";
	public string ReadWrite => "ReadWrite";

	public string LoadingPermissions => "Loading permissions...";
	public string NoUsersOrRoles => "[yellow]No users or roles found.[/]";
	public string NoRolesFound => "[yellow]No roles found.[/]";
	public string NoUsersFound => "[yellow]No users found.[/]";
	public string Username => "Username";
	public string Roles => "Roles";
	public string Role => "Role";
	public string Permissions => "Permissions";
	public string PermissionType => "Permission Type";
	public string KeyPrefix => "Key Prefix";
	public string None => "none";
	public string NoRoles => "no roles";
	public string NoPermissions => "no permissions";
}