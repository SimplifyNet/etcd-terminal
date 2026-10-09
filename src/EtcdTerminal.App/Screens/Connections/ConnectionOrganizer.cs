using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Screens.Connections;

public sealed class ConnectionOrganizer(IConnectionConfigRepository _configRepo, Menu _menu, Message _message, ILocalizationCatalog _localizations)
{
	public EtcdConnectionConfig? PickForEdit(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		var existingName = SelectInstanceName(_localizations.Current.SelectInstanceToEdit, instances);

		if (existingName is null)
			return null;

		return instances.First(i => i.Name == existingName);
	}

	public void Move(IReadOnlyList<EtcdConnectionConfig> instances, int direction)
	{
		var title = direction < 0 ? _localizations.Current.SelectInstanceToMoveUp : _localizations.Current.SelectInstanceToMoveDown;
		var name = SelectInstanceName(title, instances);

		if (name is null)
			return;

		if (direction < 0)
			_configRepo.MoveUp(name);
		else
			_configRepo.MoveDown(name);
	}

	public void Remove(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		var nameToRemove = SelectInstanceName(_localizations.Current.SelectInstanceToRemove, instances);

		if (nameToRemove is null)
			return;

		_configRepo.RemoveInstance(nameToRemove);

		_message.ShowSuccess(_localizations.Current.InstanceRemoved);
	}

	private string? SelectInstanceName(string title, IReadOnlyList<EtcdConnectionConfig> instances) =>
		_menu.Show(title, instances.Select(i => new Choice<string>(i.Name, i.Name)).ToList())?.Id;
}
