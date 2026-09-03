using EtcdTerminal.App.Engine;
using EtcdTerminal.Permissions;
using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Screens.Roles;

public sealed class PermissionTypeSelector(Menu _menu)
{
	public PermissionType? Select()
	{
		var read = LocalizationStore.Current.Read;
		var write = LocalizationStore.Current.Write;
		var readWrite = LocalizationStore.Current.ReadWrite;

		return _menu.Show(LocalizationStore.Current.SelectPermissionType, [read, write, readWrite]) switch
		{
			var c when c == read => PermissionType.Read,
			var c when c == write => PermissionType.Write,
			var c when c == readWrite => PermissionType.ReadWrite,
			_ => null
		};
	}
}
