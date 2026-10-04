using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using EtcdTerminal.Session;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Screens;

public sealed class InstanceSelectionScreen(
	IConnectionConfigRepository _configRepo,
	IDecryptFailureSource _decryptFailures,
	IConnectionWorkflow _workflow,
	SettingsScreen _settings,
	Menu _menu,
	Message _message,
	Spinner _spinner,
	ManageConnectionsScreen _manageConnections,
	ILocalization _localization)
{
	public async Task<EtcdConnectionConfig?> ShowAsync()
	{
		while (true)
		{
			var instances = _configRepo.LoadInstances();

			ShowDecryptWarningIfNeeded();

			var choice = PromptForChoice(instances);

			if (choice is null)
				return null;

			if (choice.Action is not null)
			{
				switch (choice.Action)
				{
					case InstanceFixedAction.ManageConnections:
						_manageConnections.Show(instances);
						break;
					case InstanceFixedAction.Settings:
						_settings.Show();
						break;
					case InstanceFixedAction.Exit:
						return null;
				}
			}
			else
			{
				var selected = choice.Instance!;

				bool connected;

				try
				{
					connected = await _spinner.RunAsync(_localization.Connecting, ct => _workflow.ConnectAsync(selected, ct));
				}
				catch (Exception ex)
				{
					_message.ShowError(string.Format(_localization.FailedToConnect, ex.Message));

					continue;
				}

				if (connected)
					return selected;

				_message.ShowWarning(_localization.OperationCancelled);
			}
		}
	}

	private void ShowDecryptWarningIfNeeded()
	{
		var decryptFailures = _decryptFailures.TakeDecryptFailures();

		if (decryptFailures.Count is 0)
			return;

		_message.ShowWarning(string.Format(_localization.UndecryptablePasswords, string.Join(", ", decryptFailures)));
	}

	private InstanceMenuChoice? PromptForChoice(IReadOnlyList<EtcdConnectionConfig> instances)
	{
		List<Choice<InstanceMenuChoice>> items =
		[
			.. instances.Select(i => new Choice<InstanceMenuChoice>(new(null, i), $"{i.Name}  ({i.ConnectionString})")),
			new(new(InstanceFixedAction.ManageConnections, null), _localization.ManageConnections),
			new(new(InstanceFixedAction.Settings, null), _localization.Settings),
			new(new(InstanceFixedAction.Exit, null), _localization.Exit)
		];

		List<Block>? preamble = instances.Count is 0
			? [TextBlock.Line(new StyledText(_localization.NoConnectionsMessage, TextRole.Warning))]
			: null;

		return _menu.Show(string.Empty, items, preamble)?.Id;
	}
}
