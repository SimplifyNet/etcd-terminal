using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Keys;
using EtcdTerminal.Localization;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyCreateScreen(IEtcdKeyStore _keyStore, ScreenLayout _screenLayout, Prompt _prompt, Message _message)
{
	public async Task ShowAsync()
	{
		_screenLayout.RenderHeader();

		var key = _prompt.Ask(LocalizationStore.Current.EnterKey);

		if (key is null)
			return;

		var value = _prompt.Ask(LocalizationStore.Current.EnterValue);

		if (value is null)
			return;

		var result = await _keyStore.CreateKeyAsync(key, value);

		_message.ShowResult(result, LocalizationStore.Current.KeyCreated, LocalizationStore.Current.KeyCreateFailed);
	}
}
