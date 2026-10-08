using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.MainMenu;

public sealed class MainMenuLabels(ILocalization _localization)
{
	public string For(MainMenuAction action) => action switch
	{
		MainMenuAction.BrowseKeys => _localization.BrowseKeys,
		MainMenuAction.CreateKey => _localization.CreateKey,
		MainMenuAction.ImportJson => _localization.ImportJson,
		MainMenuAction.ManageUsers => _localization.ManageUsers,
		MainMenuAction.ManageRoles => _localization.ManageRoles,
		MainMenuAction.ListPermissions => _localization.ListPermissions,
		MainMenuAction.Disconnect => _localization.Disconnect,
		_ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
	};
}
