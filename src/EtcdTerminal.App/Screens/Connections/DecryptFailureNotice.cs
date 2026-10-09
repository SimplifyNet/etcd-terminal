using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Connections;

public sealed class DecryptFailureNotice(IDecryptFailureSource _source, Message _message, ILocalizationCatalog _localizations)
{
	public void ShowIfAny()
	{
		var decryptFailures = _source.TakeDecryptFailures();

		if (decryptFailures.Count is 0)
			return;

		_message.ShowWarning(string.Format(_localizations.Current.UndecryptablePasswords, string.Join(", ", decryptFailures)));
	}
}
