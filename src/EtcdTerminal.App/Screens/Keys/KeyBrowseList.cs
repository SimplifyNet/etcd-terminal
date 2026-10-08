using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation;
using EtcdTerminal.Session;

namespace EtcdTerminal.App.Screens.Keys;

/// <summary>
/// The list state of the key browser: the loaded keys, the filter and the page
/// the control navigates. The screen only drives the loop around it.
/// </summary>
public sealed class KeyBrowseList(IReadableKeysProvider _readableKeys, IConnectionSession _session, IAppSettingsStore _settings, KeyBrowseControl _control)
{
	private readonly KeyPager _pager = new();

	public async Task OpenAsync()
	{
		_control.ResetNavigation();
		_control.ClearSearch();

		await LoadKeysAsync();
	}

	public async Task ReloadAsync()
	{
		await LoadKeysAsync();

		_pager.Filter(_control.SearchQuery);
		_control.ClampPage(_pager.GetTotalPages(_settings.Current.PageSize));
	}

	public KeyBrowseCommand ReadCommand()
	{
		var (page, totalPages) = CurrentPage();
		var next = _control.ReadCommand(page, totalPages);

		if (next.Action is KeyBrowseAction.SearchChanged)
			ApplyFilter();

		return next;
	}

	public FrameModel Frame()
	{
		var (page, totalPages) = CurrentPage();

		return _control.Frame(page, totalPages, _pager.FilteredCount);
	}

	private async Task LoadKeysAsync() =>
		_pager.SetSource(await _readableKeys.GetReadableKeysAsync(_session.Capabilities));

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
}
