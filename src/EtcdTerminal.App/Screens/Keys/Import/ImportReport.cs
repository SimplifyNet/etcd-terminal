using EtcdTerminal.App.Components;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Keys.Import;

/// <summary>
/// Reports how an import ended: fully imported, stopped by a failure or
/// cancelled by the user.
/// </summary>
public sealed class ImportReport(Message _message, ILocalizationCatalog _localizations)
{
	public void ShowImportSummary(KeyImportResult confirmed)
	{
		var summary = string.Format(_localizations.Current.ImportResult, confirmed.Created + confirmed.Overwritten, confirmed.Overwritten, confirmed.Failed);

		if (confirmed.Failed > 0)
			_message.ShowWarning(summary);
		else
			_message.ShowSuccess(summary);
	}

	public void ShowStopped(IReadOnlyList<KeyValuePair<string, string>> entries, KeyImportResult confirmed, EtcdOperationException failure)
	{
		var pendingKey = entries[confirmed.Created + confirmed.Overwritten + confirmed.Failed].Key;
		var outcome = failure.Kind is EtcdOperationFailureKind.Unconfirmed
			? string.Format(_localizations.Current.ImportEntryUnconfirmed, pendingKey, failure.Message)
			: string.Format(_localizations.Current.ImportEntryFailed, pendingKey, failure.Message);
		var partial = string.Format(_localizations.Current.ImportPartialResult, confirmed.Created, confirmed.Overwritten, confirmed.Failed);

		_message.ShowWarning(outcome + "\n" + partial);
	}

	public void ShowCancelled(IReadOnlyList<KeyValuePair<string, string>> entries, KeyImportResult confirmed)
	{
		if (confirmed.Created == 0 && confirmed.Overwritten == 0 && confirmed.Failed == 0)
		{
			_message.ShowWarning(_localizations.Current.ImportCancelled);

			return;
		}

		var pendingKey = entries[confirmed.Created + confirmed.Overwritten + confirmed.Failed].Key;
		var unconfirmed = string.Format(_localizations.Current.ImportEntryUnconfirmed, pendingKey, _localizations.Current.OperationCancelled);
		var partial = string.Format(_localizations.Current.ImportPartialResult, confirmed.Created, confirmed.Overwritten, confirmed.Failed);

		_message.ShowWarning(_localizations.Current.ImportCancelled + "\n" + unconfirmed + "\n" + partial);
	}
}
