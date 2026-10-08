using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Settings;

public sealed class PageSizeEditor(UserInput _input, SettingsWriter _writer, Message _message, ILocalization _localization)
{
	private const int MinPageSize = 1;
	private const int MaxPageSize = 500;

	public void Edit()
	{
		var input = _input.Ask(_localization.EnterPageSize);

		if (input is null)
			return;

		if (!int.TryParse(input, out var pageSize) || pageSize is not (>= MinPageSize and <= MaxPageSize))
		{
			_message.ShowError(_localization.InvalidPageSize);

			return;
		}

		var updated = new AppSettings
		{
			PageSize = pageSize,
			TrimInputValues = _writer.Current.TrimInputValues
		};

		if (_writer.TrySave(updated))
			_message.ShowSuccess(_localization.SettingsSaved);
	}
}
