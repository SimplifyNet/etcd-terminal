using EtcdTerminal.Security;

namespace EtcdTerminal.App.Screens;

public interface IMainMenuEntry
{
	MainMenuAction Action { get; }

	string Label { get; }

	bool IsAvailable(UserCapabilities capabilities);

	Task ShowAsync();
}
