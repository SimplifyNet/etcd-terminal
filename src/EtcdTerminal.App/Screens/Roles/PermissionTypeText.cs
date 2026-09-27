using EtcdTerminal.Localization;
using EtcdTerminal.Permissions;

namespace EtcdTerminal.App.Screens.Roles;

public static class PermissionTypeText
{
	public static string For(PermissionType type, ILocalization localization) => type switch
	{
		PermissionType.Read => localization.Read,
		PermissionType.Write => localization.Write,
		_ => localization.ReadWrite
	};
}
