using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Settings;

public sealed class PageSizeEditor(UserInput _input, SettingsWriter _writer, Message _message, ILocalizationCatalog _localizations) : ISettingsEntry
{
	private const int MinPageSize = 1;
	private const int MaxPageSize = 500;

	public SettingsAction Action => SettingsAction.EditPageSize;

	public string Label => $"{_localizations.Current.PageSizeLabel} ({_writer.Current.PageSize})";

	public bool Edit()
	{
		var input = _input.Ask(_localizations.Current.EnterPageSize);

		if (input is null)
			return false;

		if (!int.TryParse(input, out var pageSize) || pageSize is not (>= MinPageSize and <= MaxPageSize))
		{
			_message.ShowError(_localizations.Current.InvalidPageSize);

			return false;
		}

		if (_writer.TrySave(_writer.Current with { PageSize = pageSize }))
			_message.ShowSuccess(_localizations.Current.SettingsSaved);

		return false;
	}
}
