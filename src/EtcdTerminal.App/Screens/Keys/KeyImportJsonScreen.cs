using EtcdTerminal.App.Screens.Keys.Import;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Security;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyImportJsonScreen(ImportSourceReader _reader, ImportPreview _preview, ImportRunner _runner, ImportParseFailureNotice _failureNotice) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.ImportJson;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanWriteKeys;

	public async Task ShowAsync()
	{
		var source = await _reader.ReadAsync();

		if (source is null)
			return;

		var parsed = ImportEntriesParser.Parse(source);

		if (parsed.Failure is { } failure)
		{
			_failureNotice.Show(failure);

			return;
		}

		if (!_preview.Confirm(parsed.Entries, source.Json))
			return;

		await _runner.ImportAsync(parsed.Entries);
	}
}
