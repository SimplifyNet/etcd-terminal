namespace EtcdTerminal.App.Screens.Settings;

/// <summary>
/// One row of the settings menu: the setting it edits, its label with the
/// current value, and the edit itself.
/// </summary>
public interface ISettingsEntry
{
	SettingsAction Action { get; }

	string Label { get; }

	/// <summary>
	/// Returns whether the screens have to be rebuilt after the edit.
	/// </summary>
	bool Edit();
}
