using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Cluster;
using EtcdTerminal.Security;

namespace EtcdTerminal.App.Screens.Cluster;

/// <summary>
/// The cluster page: every member of the connected cluster as one filterable,
/// paginated table between the banner and the footer. View-only by design.
/// </summary>
public sealed class ClusterStatusScreen(IEtcdClusterStatusProvider _cluster, ClusterStatusLayout _layout, CancellableLoad _load, ListBrowser _browser) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.ClusterStatus;

	public bool IsAvailable(UserCapabilities capabilities) => true;

	public async Task ShowAsync()
	{
		var members = await _load.RunAsync(_layout.Loading, _cluster.GetMembersAsync);

		if (members is null)
			return;

		_browser.Show(_layout.Headers(), _layout.Rows(members), _layout.Empty, _layout.Total);
	}
}
