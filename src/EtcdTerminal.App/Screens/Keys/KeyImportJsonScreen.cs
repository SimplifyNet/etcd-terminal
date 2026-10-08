using EtcdTerminal.App.Screens.Keys.Import;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Security;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyImportJsonScreen(ImportSourceReader _reader, ImportEntriesParser _parser, ImportPreview _preview, ImportRunner _runner) : IMainMenuEntry
{
	public MainMenuAction Action => MainMenuAction.ImportJson;

	public bool IsAvailable(UserCapabilities capabilities) => capabilities.CanWriteKeys;

	public async Task ShowAsync()
	{
		var source = await _reader.ReadAsync();

		if (source is null)
			return;

		var entries = _parser.Parse(source);

		if (entries is null)
			return;

		if (!_preview.Confirm(entries, source.Json))
			return;

		await _runner.ImportAsync(entries);
	}
}
