using EtcdTerminal.App.Components;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Keys;

/// <summary>
/// Asks for the key and the value of a new entry on the opened screen.
/// </summary>
public sealed class KeyCreateForm(Screen _screen, UserInput _input, ILocalization _localization)
{
	public (string Key, string Value)? Ask()
	{
		_screen.Open();

		var key = _input.Ask(_localization.EnterKey);

		if (key is null)
			return null;

		var value = _input.Ask(_localization.EnterValue, allowEmpty: true);

		if (value is null)
			return null;

		return (key, value);
	}
}
