using EtcdTerminal.Keys;

namespace EtcdTerminal.App.Screens.Keys;

public readonly record struct KeyBrowseCommand(KeyBrowseAction Action, EtcdKeyValue? SelectedKey)
{
	public static KeyBrowseCommand None => new(KeyBrowseAction.None, null);
}
