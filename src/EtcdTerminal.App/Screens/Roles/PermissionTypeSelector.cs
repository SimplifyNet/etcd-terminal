using EtcdTerminal.App.Engine;
using EtcdTerminal.Permissions;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Roles;

public sealed class PermissionTypeSelector(Menu _menu, ILocalizationCatalog _localizations)
{
	public PermissionType? Select()
	{
		var read = _localizations.Current.Read;
		var write = _localizations.Current.Write;
		var readWrite = _localizations.Current.ReadWrite;

		return _menu.Ask<PermissionType>(_localizations.Current.SelectPermissionType,
		[
			new(PermissionType.Read, read),
			new(PermissionType.Write, write),
			new(PermissionType.ReadWrite, readWrite)
		])?.Id;
	}
}
