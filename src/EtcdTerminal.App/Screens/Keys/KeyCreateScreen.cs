using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyCreateScreen(ITerminal _terminal, IEtcdClient _etcdClient, ScreenLayout _screenLayout, PressAnyKeyPrompt _pressAnyKey)
{
	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		_screenLayout.RenderHeader(config);

		var key = Prompt.Ask(LocalizationStore.Current.EnterKey);

		if (key is null)
			return;

		var value = Prompt.Ask(LocalizationStore.Current.EnterValue);

		if (value is null)
			return;

		var result = await _etcdClient.CreateKeyAsync(key, value);

		if (result)
			_terminal.WriteLine(LocalizationStore.Current.KeyCreated, TerminalColor.Success);
		else
			_terminal.WriteLine(LocalizationStore.Current.KeyCreateFailed, TerminalColor.Error);

		_pressAnyKey.Show();
	}
}
