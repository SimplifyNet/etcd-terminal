using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens;
using EtcdTerminal.Configuration;
using EtcdTerminal.Security;
using EtcdTerminal.Session;
using EtcdTerminal.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Keys;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyBrowseScreen(IEtcdKeyStore _keyStore, IReadableKeysProvider _readableKeys, IConnectionSession _session, KeyBrowseControl _control, KeyBrowseLayout _layout, Prompt _prompt, Message _message, ILocalization _localization, IAppSettingsStore _settings) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.BrowseKeys;

	public string Label => _localization.BrowseKeys;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanReadKeys;

	private readonly KeyPager _pager = new();

	public async Task ShowAsync()
	{
		_control.ResetNavigation();
		_control.ClearSearch();

		await LoadKeysAsync();

		try
		{
			while (true)
			{
				var pageSize = _settings.Current.PageSize;
				var page = _pager.GetPage(_control.CurrentPage, pageSize);
				var totalPages = _pager.GetTotalPages(pageSize);

				_control.Render(page, totalPages, _pager.FilteredCount);

				var command = _control.ReadCommand(page, totalPages);

				switch (command.Action)
				{
					case KeyBrowseAction.SearchChanged:
						ApplyFilter();
						break;
					case KeyBrowseAction.Edit:
						_control.Release();
						await EditKeyAsync(command.SelectedKey!);
						break;
					case KeyBrowseAction.Delete:
						_control.Release();
						await DeleteKeyAsync(command.SelectedKey!);
						break;
					case KeyBrowseAction.Exit:
						return;
				}
			}
		}
		finally
		{
			_control.Release();
		}
	}

	private async Task LoadKeysAsync() =>
		_pager.SetSource(await _readableKeys.GetReadableKeysAsync(_session.Capabilities));

	private async Task EditKeyAsync(EtcdKeyValue key)
	{
		_control.ShowDetails(
			_layout.Detail(_localization.EditingKey, key.Key, TextRole.Primary),
			_layout.Detail(_localization.CurrentValue, ValuePreview.Sanitize(key.Value), TextRole.Success));

		var newValue = _prompt.Ask(_localization.EnterNewValue, key.Value, trim: _settings.Current.TrimInputValues);

		if (newValue is null)
			return;

		var result = await _keyStore.UpdateKeyAsync(key.Key, newValue);

		if (result)
			await ReloadAsync();

		_message.ShowResult(result, _localization.KeyUpdated, _localization.CouldNotUpdateKey);
	}

	private async Task DeleteKeyAsync(EtcdKeyValue key)
	{
		_control.ShowDetails(_layout.Detail(_localization.DeleteKey, key.Key, TextRole.Danger));

		var result = await _keyStore.DeleteKeyAsync(key.Key);

		if (result)
			await ReloadAsync();

		_message.ShowResult(result, _localization.KeyDeleted, _localization.KeyCouldNotBeDeleted);
	}

	private async Task ReloadAsync()
	{
		await LoadKeysAsync();

		_pager.Filter(_control.SearchQuery);
		_control.ClampPage(_pager.GetTotalPages(_settings.Current.PageSize));
	}

	private void ApplyFilter()
	{
		_pager.Filter(_control.SearchQuery);
		_control.ResetNavigation();
	}
}
