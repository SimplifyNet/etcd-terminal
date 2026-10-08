using EtcdTerminal.App.Components;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Keys.Import;

/// <summary>
/// Runs the import behind the spinner and hands the outcome to the report.
/// </summary>
public sealed class ImportRunner(IKeyImporter _importer, Spinner _spinner, ImportReport _report, ILocalization _localization)
{
	public async Task ImportAsync(IReadOnlyList<KeyValuePair<string, string>> entries)
	{
		KeyImportResult confirmed = default;

		bool completed;

		try
		{
			completed = await _spinner.RunAsync(string.Format(_localization.ImportingKeys, entries.Count), async ct =>
			{
				confirmed = await _importer.ImportAsync(entries, snapshot => confirmed = snapshot, ct);
			});
		}
		catch (EtcdOperationException ex)
		{
			_report.ShowStopped(entries, confirmed, ex);

			return;
		}

		if (!completed)
		{
			_report.ShowCancelled(entries, confirmed);

			return;
		}

		_report.ShowImportSummary(confirmed);
	}
}
