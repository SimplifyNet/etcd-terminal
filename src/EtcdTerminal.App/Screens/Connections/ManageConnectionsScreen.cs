using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Screens.Connections;

public sealed class ManageConnectionsScreen(Menu _menu, ConnectionEditor _editor, ConnectionOrganizer _organizer, ILocalization _localization)
{
	public void Show(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		List<Choice<ManageConnectionsAction>> actions = [new(ManageConnectionsAction.AddInstance, _localization.AddInstance)];

		if (instances.Count > 0)
		{
			actions.Add(new(ManageConnectionsAction.EditInstance, _localization.EditInstance));
			actions.Add(new(ManageConnectionsAction.RemoveInstance, _localization.RemoveInstance));
		}

		if (instances.Count > 1)
		{
			actions.Add(new(ManageConnectionsAction.MoveUpInstance, _localization.MoveUpInstance));
			actions.Add(new(ManageConnectionsAction.MoveDownInstance, _localization.MoveDownInstance));
		}

		ManageConnectionsAction? action = _menu.Show(_localization.ManageConnections, actions)?.Id;

		if (action is null)
			return;

		switch (action)
		{
			case ManageConnectionsAction.AddInstance:
				_editor.Add();
				break;
			case ManageConnectionsAction.EditInstance:
				EditInstance(instances);
				break;
			case ManageConnectionsAction.MoveUpInstance:
				_organizer.Move(instances, -1);
				break;
			case ManageConnectionsAction.MoveDownInstance:
				_organizer.Move(instances, 1);
				break;
			case ManageConnectionsAction.RemoveInstance:
				_organizer.Remove(instances);
				break;
		}
	}

	private void EditInstance(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		var existing = _organizer.PickForEdit(instances);

		if (existing is null)
			return;

		_editor.Edit(existing);
	}
}
