using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Keys.Import;

/// <summary>
/// Shows why a JSON document produced no entries: the parse failure decides the
/// text, this notice only picks the warning/error severity and the localization.
/// </summary>
public sealed class ImportParseFailureNotice(Message _message, ILocalizationCatalog _localizations)
{
	public void Show(ImportParseFailure failure)
	{
		if (failure.Kind is ImportParseFailureKind.NoKeys)
			_message.ShowWarning(_localizations.Current.NoKeysInJson);
		else
			_message.ShowError(string.Format(_localizations.Current.InvalidJson, failure.Detail));
	}
}
