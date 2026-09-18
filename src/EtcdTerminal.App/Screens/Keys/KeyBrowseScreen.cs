using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens;
using EtcdTerminal.Configuration;
using EtcdTerminal.Security;
using EtcdTerminal.Session;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;
using EtcdTerminal.Keys;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyBrowseScreen(ITerminalOutput _terminal, IEtcdKeyStore _keyStore, IReadableKeysProvider _readableKeys, IConnectionSession _session, ScreenLayout _screenLayout, KeyBrowseControl _control, Prompt _prompt, Message _message, ILocalization _localization, IAppSettingsStore _settings) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.BrowseKeys;

	public string Label => _localization.BrowseKeys;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanReadKeys;
	private const int EditValueMaxLength = 200;

	private readonly KeyPager _pager = new();

	public async Task ShowAsync()
	{
		_control.ResetNavigation();
		_control.ClearSearch();

		await LoadKeysAsync();

		while (true)
		{
			var pageSize = _settings.Current.PageSize;

			_control.Render(_pager.GetPage(_control.CurrentPage, pageSize), _pager.GetTotalPages(pageSize), _pager.FilteredCount);

			var command = _control.ReadCommand(_pager.GetPage(_control.CurrentPage, pageSize), _pager.GetTotalPages(pageSize));

			switch (command.Action)
			{
				case KeyBrowseAction.SearchChanged:
					ApplyFilter();
					break;
				case KeyBrowseAction.Edit:
					await EditKeyAsync(command.SelectedKey!);
					break;
				case KeyBrowseAction.Delete:
					await DeleteKeyAsync(command.SelectedKey!);
					break;
				case KeyBrowseAction.Exit:
					return;
			}
		}
	}

	private async Task LoadKeysAsync() =>
		_pager.SetSource(await _readableKeys.GetReadableKeysAsync(_session.Capabilities));

	private async Task EditKeyAsync(EtcdKeyValue key)
	{
		_screenLayout.RenderHeader();
		_terminal.Write($"{_localization.EditingKey} ");
		_terminal.WriteLine(key.Key, TerminalColor.Primary);
		_terminal.Write($"{_localization.CurrentValue} ");
		_terminal.WriteLine(KeyBrowseLayout.TruncateText(key.Value, EditValueMaxLength), TerminalColor.Success);
		_terminal.WriteLine();

		var newValue = _prompt.Ask(_localization.EnterNewValue, key.Value);

		if (newValue is null)
			return;

		var result = await _keyStore.UpdateKeyAsync(key.Key, newValue);

		_screenLayout.RenderHeader();

		if (result)
			await ReloadAsync();

		_message.ShowResult(result, _localization.KeyUpdated, _localization.CouldNotUpdateKey);
	}

	private async Task DeleteKeyAsync(EtcdKeyValue key)
	{
		_screenLayout.RenderHeader();
		_terminal.Write($"{_localization.DeleteKey} ");
		_terminal.WriteLine(key.Key, TerminalColor.Danger);
		_terminal.WriteLine();

		var result = await _keyStore.DeleteKeyAsync(key.Key);

		_screenLayout.RenderHeader();

		if (result)
			await ReloadAsync();

		_message.ShowResult(result, _localization.KeyDeleted, _localization.KeyCouldNotBeDeleted);
	}

	private async Task ReloadAsync()
	{
		await LoadKeysAsync();

		_control.ClampPage(_pager.GetTotalPages(_settings.Current.PageSize));
	}

	private void ApplyFilter()
	{
		_pager.Filter(_control.SearchQuery);
		_control.ResetNavigation();
	}
}
