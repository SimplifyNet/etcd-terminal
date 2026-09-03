using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Localization;
using Spectre.Console;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyCreateScreen(IEtcdClient _etcdClient)
{
	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		ScreenLayout.RenderHeader(config);

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

		PressAnyKeyPrompt.Show();
	}
}