using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Session;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;
using EtcdTerminal.Keys;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyBrowseScreen(ITerminal _terminal, IEtcdKeyStore _keyStore, IReadableKeysProvider _readableKeys, IConnectionSession _session, ScreenLayout _screenLayout, KeyBrowseControl _control, Prompt _prompt, Message _message)
{
	private const int EditValueMaxLength = 200;

	private List<EtcdKeyValue> _allKeys = [];
	private List<EtcdKeyValue> _filteredKeys = [];

	public async Task ShowAsync()
	{
		_control.ResetNavigation();
		_control.ClearSearch();

		await LoadKeysAsync();

		while (true)
		{
			_control.Render(GetCurrentPageKeys(), GetTotalPages(), _filteredKeys.Count);

			var command = _control.ReadCommand(GetCurrentPageKeys(), GetTotalPages());

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

	private async Task LoadKeysAsync()
	{
		_allKeys = [.. await _readableKeys.GetReadableKeysAsync(_session.Active?.Username)];
		_filteredKeys = [.. _allKeys];
	}

	private async Task EditKeyAsync(EtcdKeyValue key)
	{
		_screenLayout.RenderHeader();
		_terminal.Write($"{LocalizationStore.Current.EditingKey} ");
		_terminal.WriteLine(key.Key, TerminalColor.Default);
		_terminal.Write($"{LocalizationStore.Current.CurrentValue} ");
		_terminal.WriteLine(KeyBrowseLayout.TruncateText(key.Value, EditValueMaxLength), TerminalColor.Success);
		_terminal.WriteLine();

		var newValue = _prompt.Ask(LocalizationStore.Current.EnterNewValue, key.Value);

		if (newValue is null)
			return;

		var result = await _keyStore.UpdateKeyAsync(key.Key, newValue);

		_screenLayout.RenderHeader();

		if (result)
			await ReloadAsync();

		_message.ShowResult(result, LocalizationStore.Current.KeyUpdated, LocalizationStore.Current.CouldNotUpdateKey);
	}

	private async Task DeleteKeyAsync(EtcdKeyValue key)
	{
		_screenLayout.RenderHeader();
		_terminal.Write($"{LocalizationStore.Current.DeleteKey} ");
		_terminal.WriteLine(key.Key, TerminalColor.Error);
		_terminal.WriteLine();

		var result = await _keyStore.DeleteKeyAsync(key.Key);

		_screenLayout.RenderHeader();

		if (result)
			await ReloadAsync();

		_message.ShowResult(result, LocalizationStore.Current.KeyDeleted, LocalizationStore.Current.KeyCouldNotBeDeleted);
	}

	private async Task ReloadAsync()
	{
		await LoadKeysAsync();

		_control.ClampPage(GetTotalPages());
	}

	private void ApplyFilter()
	{
		if (string.IsNullOrEmpty(_control.SearchQuery))
			_filteredKeys = [.. _allKeys];
		else
		{
			var query = _control.SearchQuery;

			_filteredKeys = [.. _allKeys
				.Where(kv =>
					kv.Key.Contains(query, StringComparison.OrdinalIgnoreCase) ||
					kv.Value.Contains(query, StringComparison.OrdinalIgnoreCase))];
		}

		_control.ResetNavigation();
	}

	private List<EtcdKeyValue> GetCurrentPageKeys()
	{
		var pageSize = AppSettingsStore.Current.PageSize;
		var start = _control.CurrentPage * pageSize;

		return [.. _filteredKeys.Skip(start).Take(pageSize)];
	}

	private int GetTotalPages() =>
		_filteredKeys.Count == 0 ? 1 : (int)Math.Ceiling((double)_filteredKeys.Count / AppSettingsStore.Current.PageSize);
}
