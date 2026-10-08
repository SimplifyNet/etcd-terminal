using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Presentation;
using EtcdTerminal.Security;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyBrowseScreen(KeyBrowseList _list, KeyChanges _changes, Screen _screen, ILiveFrame _live) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.BrowseKeys;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanReadKeys;

	public async Task ShowAsync()
	{
		await _list.OpenAsync();

		while (true)
		{
			_screen.Reset();

			var command = RunLiveLoop();

			if (!await HandleCommandAsync(command))
				return;
		}
	}

	private KeyBrowseCommand RunLiveLoop() =>
		_live.Run(_list.Frame(), LiveFrameEnd.Clear, updater =>
		{
			while (true)
			{
				var next = _list.ReadCommand();

				if (next.Action is KeyBrowseAction.Edit or KeyBrowseAction.Delete or KeyBrowseAction.Exit)
					return next;

				updater.Update(_list.Frame());
			}
		});

	private async Task<bool> HandleCommandAsync(KeyBrowseCommand command)
	{
		switch (command.Action)
		{
			case KeyBrowseAction.Edit:
				await _changes.EditAsync(command.SelectedKey!, _list.ReloadAsync);
				break;
			case KeyBrowseAction.Delete:
				await _changes.DeleteAsync(command.SelectedKey!, _list.ReloadAsync);
				break;
			case KeyBrowseAction.Exit:
				return false;
		}

		return true;
	}
}
