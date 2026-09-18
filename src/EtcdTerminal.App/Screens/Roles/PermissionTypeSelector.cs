using EtcdTerminal.App.Engine;
using EtcdTerminal.Permissions;
using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Screens.Roles;

public sealed class PermissionTypeSelector(Menu _menu, ILocalization _localization)
{
	public PermissionType? Select()
	{
		var read = _localization.Read;
		var write = _localization.Write;
		var readWrite = _localization.ReadWrite;

		return _menu.Show<PermissionType>(_localization.SelectPermissionType,
		[
			new(PermissionType.Read, read),
			new(PermissionType.Write, write),
			new(PermissionType.ReadWrite, readWrite)
		])?.Id;
	}
}
