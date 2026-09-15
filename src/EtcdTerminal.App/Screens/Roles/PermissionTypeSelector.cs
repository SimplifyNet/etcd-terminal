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

		return _menu.Show<PermissionType>(LocalizationStore.Current.SelectPermissionType,
		[
			new(PermissionType.Read, read),
			new(PermissionType.Write, write),
			new(PermissionType.ReadWrite, readWrite)
		]);
	}
}
