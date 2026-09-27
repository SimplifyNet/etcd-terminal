using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Session;
using EtcdTerminal.Localization;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Screens;

public sealed class MainScreen(
	ScreenLayout _screenLayout,
	IEtcdConnection _connection,
	IConnectionSession _session,
	IEnumerable<IMainMenuEntry> _entries,
	Menu _menu,
	ILocalization _localization)
{
	public async Task ShowAsync()
	{
		try
		{
			await RunMenuLoopAsync();
		}
		catch
		{
			await DisconnectIgnoringErrorsAsync();

			throw;
		}

		await DisconnectAsync();
	}

	private async Task RunMenuLoopAsync()
	{
		while (true)
		{
			_screenLayout.RenderHeader();

			MainMenuAction? action = _menu.Show(string.Empty, BuildMenuItems())?.Id;

			if (action is null)
				return;

			var entry = _entries.FirstOrDefault(e => e.Action == action);

			if (entry is null)
				return;

			await entry.ShowAsync();
		}
	}

	/// <summary>
	/// Only shows the actions the connected account is actually permitted to perform.
	/// </summary>
	private List<MenuItem<MainMenuAction>> BuildMenuItems()
	{
		List<MenuItem<MainMenuAction>> items = [.. _entries
			.Where(e => e.IsAvailable(_session.Capabilities))
			.Select(e => new MenuItem<MainMenuAction>(e.Action, e.Label))];

		items.Add(new(MainMenuAction.Disconnect, _localization.Disconnect));

		return items;
	}

	private async Task DisconnectAsync()
	{
		_session.End();

		await _connection.DisconnectAsync();
	}

	private async Task DisconnectIgnoringErrorsAsync()
	{
		_session.End();

		try
		{
			await _connection.DisconnectAsync();
		}
		catch
		{
			// The session is already cleared. A failed disconnect must not
			// mask the menu error that is currently propagating.
		}
	}
}
