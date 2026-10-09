using EtcdTerminal.App.Engine;
using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Settings;

public sealed class SettingsScreen(Menu _menu, ICollection<ISettingsEntry> _entries, ILocalizationCatalog _localizations)
{
	/// <summary>
	/// Returns whether the screens have to be rebuilt, which is the case
	/// after a language change.
	/// </summary>
	public bool Show()
	{
		while (true)
		{
			var action = _menu.Show(_localizations.Current.SettingsTitle, _entries.Select(entry => new Choice<SettingsAction>(entry.Action, entry.Label)).ToArray())?.Id;

			if (action is null)
				return false;

			if (_entries.Single(entry => entry.Action == action).Edit())
				return true;
		}
	}
}
