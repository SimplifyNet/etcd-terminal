using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Settings;

public sealed class TrimInputValuesEditor(SettingsWriter _writer, ILocalizationCatalog _localizations) : ISettingsEntry
{
	public SettingsAction Action => SettingsAction.ToggleTrimInputValues;

	public string Label => $"{_localizations.Current.TrimInputValuesLabel} ({(_writer.Current.TrimInputValues ? _localizations.Current.On : _localizations.Current.Off)})";

	public bool Edit()
	{
		_writer.TrySave(_writer.Current with { TrimInputValues = !_writer.Current.TrimInputValues });

		return false;
	}
}
