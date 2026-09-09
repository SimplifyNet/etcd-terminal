using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyCreateScreen(ITerminal _terminal, IEtcdClient _etcdClient, ScreenLayout _screenLayout, PressAnyKeyPrompt _pressAnyKey, Prompt _prompt)
{
	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		_screenLayout.RenderHeader(config);

		var key = _prompt.Ask(LocalizationStore.Current.EnterKey);

		if (key is null)
			return;

		var value = _prompt.Ask(LocalizationStore.Current.EnterValue);

		if (value is null)
			return;

		var result = await _etcdClient.CreateKeyAsync(key, value);

		_terminal.WriteLine();

		if (result)
			_terminal.WriteIndentedLine(LocalizationStore.Current.KeyCreated, TerminalColor.Success);
		else
			_terminal.WriteIndentedLine(LocalizationStore.Current.KeyCreateFailed, TerminalColor.Error);

		_terminal.WriteLine();
		_pressAnyKey.Show();
	}
}
