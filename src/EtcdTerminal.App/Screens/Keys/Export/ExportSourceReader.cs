using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Keys.Export;

/// <summary>
/// Reads the key prefix of an export. An empty prefix exports every readable key.
/// </summary>
public sealed class ExportSourceReader(Screen _screen, UserInput _input, ILocalizationCatalog _localizations)
{
	public ExportSource? Read()
	{
		_screen.Open();

		var prefix = _input.Ask(_localizations.Current.EnterExportPrefix, allowEmpty: true);

		if (prefix is null)
			return null;

		return new(prefix);
	}
}
