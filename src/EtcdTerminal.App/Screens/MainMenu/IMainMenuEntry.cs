using EtcdTerminal.Security;

namespace EtcdTerminal.App.Screens.MainMenu;

public interface IMainMenuEntry
{
	MainMenuAction Action { get; }

	string Label { get; }

	bool IsAvailable(UserCapabilities capabilities);

	Task ShowAsync();
}
