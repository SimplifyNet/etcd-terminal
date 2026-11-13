using EtcdTerminal.App.Components;
using EtcdTerminal.Clipboard;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Keys.Export;

/// <summary>
/// Turns the selected keys into JSON, asks the clipboard to take it and reports
/// how the export ended.
/// </summary>
public sealed class ExportReport(Message _message, IClipboard _clipboard, ILocalizationCatalog _localizations)
{
	public void ShowNoKeys() => _message.ShowWarning(_localizations.Current.ExportNoKeys);

	public void ShowCancelled() => _message.ShowWarning(_localizations.Current.ExportCancelled);

	public void ShowStopped(EtcdOperationException failure) =>
		_message.ShowError(string.Format(_localizations.Current.ExportFailed, failure.Message));

	public void ShowExported(IReadOnlyList<EtcdKeyValue> keys)
	{
		var json = JsonKeyExporter.Export(keys);

		if (_clipboard.Copy(json))
			_message.ShowSuccess(string.Format(_localizations.Current.ExportResult, keys.Count));
		else
			_message.ShowError(_localizations.Current.ExportClipboardUnavailable);
	}
}
