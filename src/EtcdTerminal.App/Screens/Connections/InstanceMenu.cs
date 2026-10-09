using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Screens.Connections;

public sealed class InstanceMenu(IConnectionConfigRepository _configRepo, DecryptFailureNotice _notice, Menu _menu, ILocalizationCatalog _localizations)
{
	public InstanceMenuResult Show()
	{
		var instances = _configRepo.LoadInstances();

		_notice.ShowIfAny();

		return new(instances, PromptForChoice(instances));
	}

	private InstanceMenuChoice? PromptForChoice(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		// The selection prompt cannot render a non-selectable row without
		// indenting everything offered after it, so the separator is the trailing
		// newline of the last instance row: the same blank line and indentation,
		// and the cursor never lands on it.
		List<Choice<InstanceMenuChoice>> items =
		[
			.. instances.Select((instance, index) => new Choice<InstanceMenuChoice>(
				new(null, instance),
				$"{instance.Name}  ({instance.ConnectionString}){(index == instances.Count - 1 ? "\n" : string.Empty)}")),
			new(new(InstanceFixedAction.ManageConnections, null), _localizations.Current.ManageConnections),
			new(new(InstanceFixedAction.Settings, null), _localizations.Current.Settings),
			new(new(InstanceFixedAction.Exit, null), _localizations.Current.Exit)
		];

		List<Block>? preamble = instances.Count is 0
			? [TextBlock.Line(new StyledText(_localizations.Current.NoConnectionsMessage, TextRole.Warning))]
			: null;

		return _menu.Show(string.Empty, items, preamble)?.Id;
	}
}
