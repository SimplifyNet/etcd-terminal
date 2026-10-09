using EtcdTerminal.App.Components;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Keys;

/// <summary>
/// Applies an edit or a delete and reports the outcome. The reload happens
/// before the message, so the result the user reads shows the stored value.
/// </summary>
public sealed class KeyChanges(IEtcdKeyStore _keyStore, KeyEditPrompt _prompt, Message _message, ILocalizationCatalog _localizations)
{
	public async Task EditAsync(EtcdKeyValue key, Func<Task> reload)
	{
		var newValue = _prompt.AskNewValue(key);

		if (newValue is null)
			return;

		var result = await _keyStore.UpdateKeyAsync(key.Key, newValue);

		if (result)
			await reload();

		_message.ShowResult(result, _localizations.Current.KeyUpdated, _localizations.Current.CouldNotUpdateKey);
	}

	public async Task DeleteAsync(EtcdKeyValue key, Func<Task> reload)
	{
		_prompt.ShowDeleting(key);

		var result = await _keyStore.DeleteKeyAsync(key.Key);

		if (result)
			await reload();

		_message.ShowResult(result, _localizations.Current.KeyDeleted, _localizations.Current.KeyCouldNotBeDeleted);
	}
}
