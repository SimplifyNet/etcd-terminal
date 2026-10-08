using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Configuration;
using EtcdTerminal.Security;
using EtcdTerminal.Session;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;
using EtcdTerminal.Keys;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyBrowseScreen(IEtcdKeyStore _keyStore, IReadableKeysProvider _readableKeys, IConnectionSession _session, KeyBrowseControl _control, KeyBrowseLayout _layout, UserInput _input, Message _message, ILocalization _localization, IAppSettingsStore _settings, Screen _screen, ILiveFrame _live) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.BrowseKeys;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanReadKeys;

	private readonly KeyPager _pager = new();

	public async Task ShowAsync()
	{
		_control.ResetNavigation();
		_control.ClearSearch();

		await LoadKeysAsync();

		while (true)
		{
			_screen.Reset();

			var command = RunLiveLoop();

			if (!await HandleCommandAsync(command))
				return;
		}
	}

	private KeyBrowseCommand RunLiveLoop() =>
		_live.Run(Frame(), LiveFrameEnd.Clear, updater =>
		{
			while (true)
			{
				var (page, totalPages) = CurrentPage();
				var next = _control.ReadCommand(page, totalPages);

				if (next.Action is KeyBrowseAction.SearchChanged)
					ApplyFilter();

				if (next.Action is KeyBrowseAction.Edit or KeyBrowseAction.Delete or KeyBrowseAction.Exit)
					return next;

				updater.Update(Frame());
			}
		});

	private async Task<bool> HandleCommandAsync(KeyBrowseCommand command)
	{
		switch (command.Action)
		{
			case KeyBrowseAction.Edit:
				await EditKeyAsync(command.SelectedKey!);
				break;
			case KeyBrowseAction.Delete:
				await DeleteKeyAsync(command.SelectedKey!);
				break;
			case KeyBrowseAction.Exit:
				return false;
		}

		return true;
	}

	private async Task LoadKeysAsync() =>
		_pager.SetSource(await _readableKeys.GetReadableKeysAsync(_session.Capabilities));

	private async Task EditKeyAsync(EtcdKeyValue key)
	{
		_screen.Open(
		[
			_layout.Detail(_localization.EditingKey, key.Key, TextRole.Primary),
			_layout.Detail(_localization.CurrentValue, DisplayText.Sanitize(key.Value), TextRole.Success)
		]);

		var newValue = _input.Ask(_localization.EnterNewValue, key.Value);

		if (newValue is null)
			return;

		var result = await _keyStore.UpdateKeyAsync(key.Key, newValue);

		if (result)
			await ReloadAsync();

		_message.ShowResult(result, _localization.KeyUpdated, _localization.CouldNotUpdateKey);
	}

	private async Task DeleteKeyAsync(EtcdKeyValue key)
	{
		_screen.Open([_layout.Detail(_localization.DeleteKey, key.Key, TextRole.Danger)]);

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

	private (IReadOnlyList<EtcdKeyValue> Page, int TotalPages) CurrentPage()
	{
		var pageSize = _settings.Current.PageSize;

		return (_pager.GetPage(_control.CurrentPage, pageSize), _pager.GetTotalPages(pageSize));
	}

	private FrameModel Frame()
	{
		var (page, totalPages) = CurrentPage();

		return _control.Frame(page, totalPages, _pager.FilteredCount);
	}
}
