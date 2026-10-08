using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Security;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyCreateScreen(IEtcdKeyStore _keyStore, KeyCreateForm _form, Message _message, ILocalization _localization) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.CreateKey;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanWriteKeys;

	public async Task ShowAsync()
	{
		if (_form.Ask() is not { } created)
			return;

		var result = await _keyStore.CreateKeyAsync(created.Key, created.Value);

		_message.ShowResult(result, _localization.KeyCreated, _localization.KeyCreateFailed);
	}
}
