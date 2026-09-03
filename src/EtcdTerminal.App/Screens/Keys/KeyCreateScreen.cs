using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using Spectre.Console;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyCreateScreen(IEtcdClient _etcdClient, ScreenLayout _screenLayout, PressAnyKeyPrompt _pressAnyKey)
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
			AnsiConsole.MarkupLine(LocalizationStore.Current.KeyCreated);
		else
			AnsiConsole.MarkupLine(LocalizationStore.Current.KeyCreateFailed);

		_pressAnyKey.Show();
	}
}
