using EtcdTerminal.App.Engine;
using EtcdTerminal.Permissions;
using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Screens.Roles;

public static class PermissionTypeSelector
{
	public static PermissionType? Select()
	{
		var read = LocalizationStore.Current.Read;
		var write = LocalizationStore.Current.Write;
		var readWrite = LocalizationStore.Current.ReadWrite;

		return Menu.Show(LocalizationStore.Current.SelectPermissionType, [read, write, readWrite]) switch
		{
			var c when c == read => PermissionType.Read,
			var c when c == write => PermissionType.Write,
			var c when c == readWrite => PermissionType.ReadWrite,
			_ => null
		};
	}
}