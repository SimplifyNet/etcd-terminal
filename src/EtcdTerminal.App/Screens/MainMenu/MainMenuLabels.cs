using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.MainMenu;

public sealed class MainMenuLabels(ILocalizationCatalog _localizations)
{
	public string For(MainMenuAction action) => action switch
	{
		MainMenuAction.BrowseKeys => _localizations.Current.BrowseKeys,
		MainMenuAction.CreateKey => _localizations.Current.CreateKey,
		MainMenuAction.ImportJson => _localizations.Current.ImportJson,
		MainMenuAction.ManageUsers => _localizations.Current.ManageUsers,
		MainMenuAction.ManageRoles => _localizations.Current.ManageRoles,
		MainMenuAction.ListPermissions => _localizations.Current.ListPermissions,
		MainMenuAction.Disconnect => _localizations.Current.Disconnect,
		_ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
	};
}
