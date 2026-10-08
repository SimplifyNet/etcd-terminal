using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Keys.Import;

/// <summary>
/// Reads the separator, the prefix and the pasted JSON of an import.
/// </summary>
public sealed class ImportSourceReader(Screen _screen, UserInput _input, MultiLinePasteReader _pasteReader, ILocalization _localization)
{
	public async Task<ImportSource?> ReadAsync()
	{
		_screen.Open();

		var separator = _input.Ask(_localization.EnterSeparator, ":");

		if (separator is null)
			return null;

		var prefix = _input.Ask(_localization.EnterPrefix, allowEmpty: true);

		if (prefix is null)
			return null;

		var json = await _pasteReader.ReadAsync(_localization.PasteJson);

		if (json is null)
			return null;

		return new(separator, prefix, json);
	}
}
