using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens;
using EtcdTerminal.Configuration;
using EtcdTerminal.Keys;
using EtcdTerminal.Localization;
using EtcdTerminal.Security;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyCreateScreen(IEtcdKeyStore _keyStore, ScreenShell _shell, Prompt _prompt, Message _message, ILocalization _localization, IAppSettingsStore _settings) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.CreateKey;

	public string Label => _localization.CreateKey;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanWriteKeys;
	public async Task ShowAsync()
	{
		_shell.Show();

		var trim = _settings.Current.TrimInputValues;
		var key = _prompt.Ask(_localization.EnterKey, trim: trim);

		if (key is null)
			return;

		var value = _prompt.Ask(_localization.EnterValue, allowEmpty: true, trim: trim);

		if (value is null)
			return;

		var result = await _keyStore.CreateKeyAsync(key, value);

		_message.ShowResult(result, _localization.KeyCreated, _localization.KeyCreateFailed);
	}
}
