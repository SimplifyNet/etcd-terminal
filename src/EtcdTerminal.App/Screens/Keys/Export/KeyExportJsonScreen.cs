using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Keys;
using EtcdTerminal.Security;

namespace EtcdTerminal.App.Screens.Keys.Export;

/// <summary>
/// Exports the readable keys under a prefix as a JSON object and puts it on the
/// clipboard: ask for the prefix, show the key names, confirm, then copy.
/// </summary>
public sealed class KeyExportJsonScreen(
	ExportSourceReader _reader,
	ExportKeysLoader _loader,
	ExportPreview _preview,
	ExportReport _report) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.ExportJson;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanReadKeys;

	public async Task ShowAsync()
	{
		var source = _reader.Read();

		if (source is null)
			return;

		IReadOnlyList<EtcdKeyValue>? keys;

		try
		{
			keys = await _loader.LoadAsync(source.Prefix);
		}
		catch (EtcdOperationException ex)
		{
			_report.ShowStopped(ex);

			return;
		}

		if (keys is null)
			return;

		if (keys.Count is 0)
		{
			_report.ShowNoKeys();

			return;
		}

		if (!_preview.Confirm(keys, source.Prefix))
		{
			_report.ShowCancelled();

			return;
		}

		_report.ShowExported(keys);
	}
}
