using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Security;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyCreateScreen(IEtcdKeyStore _keyStore, Screen _screen, UserInput _input, Message _message, ILocalization _localization) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.CreateKey;

	public string Label => _localization.CreateKey;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanWriteKeys;
	public async Task ShowAsync()
	{
		_screen.Open();

		var key = _input.Ask(_localization.EnterKey);

		if (key is null)
			return;

		var value = _input.Ask(_localization.EnterValue, allowEmpty: true);

		if (value is null)
			return;

		var result = await _keyStore.CreateKeyAsync(key, value);

		_message.ShowResult(result, _localization.KeyCreated, _localization.KeyCreateFailed);
	}
}
