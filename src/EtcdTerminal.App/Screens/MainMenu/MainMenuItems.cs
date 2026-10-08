using EtcdTerminal.Presentation;
using EtcdTerminal.Session;

namespace EtcdTerminal.App.Screens.MainMenu;

public sealed class MainMenuItems(IEnumerable<IMainMenuEntry> _entries, IConnectionSession _session, MainMenuLabels _labels)
{
	/// Only the actions the connected account may perform, then Disconnect.
	public List<Choice<MainMenuAction>> Build()
	{
		List<Choice<MainMenuAction>> items = [.. _entries
			.Where(e => e.IsAvailable(_session.Capabilities))
			.Select(e => new Choice<MainMenuAction>(e.Action, _labels.For(e.Action)))];

		items.Add(new(MainMenuAction.Disconnect, _labels.For(MainMenuAction.Disconnect)));

		return items;
	}

	public IMainMenuEntry? Find(MainMenuAction action) => _entries.FirstOrDefault(e => e.Action == action);
}
