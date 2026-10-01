using EtcdTerminal.Permissions;
using EtcdTerminal.Presentation.Localization;

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
