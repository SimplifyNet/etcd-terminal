using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Settings;

public sealed class CaseSensitiveSearchEditor(SettingsWriter _writer, ILocalizationCatalog _localizations) : ISettingsEntry
{
	public SettingsAction Action => SettingsAction.ToggleCaseSensitiveSearch;

	public string Label => $"{_localizations.Current.CaseSensitiveSearchLabel} ({(_writer.Current.CaseSensitiveSearch ? _localizations.Current.On : _localizations.Current.Off)})";

	public bool Edit()
	{
		_writer.TrySave(_writer.Current with { CaseSensitiveSearch = !_writer.Current.CaseSensitiveSearch });

		return false;
	}
}
