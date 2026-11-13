using EtcdTerminal.App.Components;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Session;

namespace EtcdTerminal.App.Screens.Keys.Export;

/// <summary>
/// Loads the readable keys and keeps the ones under the requested prefix.
/// Cancellation is reported by the loader; failures reach the caller unchanged.
/// </summary>
public sealed class ExportKeysLoader(
	IReadableKeysProvider _keys,
	IConnectionSession _session,
	CancellableLoad _load,
	ILocalizationCatalog _localizations)
{
	public async Task<IReadOnlyList<EtcdKeyValue>?> LoadAsync(string prefix)
	{
		var keys = await _load.RunAsync(
			_localizations.Current.LoadingExportKeys,
			ct => _keys.GetReadableKeysAsync(_session.Capabilities, ct));

		if (keys is null)
			return null;

		return [.. keys.Where(kv => kv.Key.StartsWith(prefix, StringComparison.Ordinal))];
	}
}
