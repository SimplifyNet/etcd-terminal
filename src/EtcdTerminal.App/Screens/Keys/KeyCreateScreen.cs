using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens;
using EtcdTerminal.Keys;
using EtcdTerminal.Localization;
using EtcdTerminal.Security;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyCreateScreen(IEtcdKeyStore _keyStore, ScreenLayout _screenLayout, Prompt _prompt, Message _message, ILocalization _localization) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.CreateKey;

	public string Label => _localization.CreateKey;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanWriteKeys;
	public async Task ShowAsync()
	{
		_screenLayout.RenderHeader();

		var key = _prompt.Ask(_localization.EnterKey);

		if (key is null)
			return;

		var value = _prompt.Ask(_localization.EnterValue);

		if (value is null)
			return;

		var result = await _keyStore.CreateKeyAsync(key, value);

		_message.ShowResult(result, _localization.KeyCreated, _localization.KeyCreateFailed);
	}
}
