using EtcdTerminal.App.Engine;
using EtcdTerminal.Permissions;

namespace EtcdTerminal.App.Screens.Roles;

public static class PermissionTypeSelector
{
	private const string SelectPermissionType = "Select permission type:";

	public static PermissionType? Select() => Menu.Show(SelectPermissionType, ["Read", "Write", "ReadWrite"]) switch
	{
		"Read" => PermissionType.Read,
		"Write" => PermissionType.Write,
		"ReadWrite" => PermissionType.ReadWrite,
		_ => null
	};
}
