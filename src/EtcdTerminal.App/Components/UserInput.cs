using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;

namespace EtcdTerminal.App.Components;

/// <summary>
/// Asks the user for text with the trimming policy the user configured.
/// </summary>
public sealed class UserInput(Prompt _prompt, IAppSettingsStore _settings)
{
	private bool Trim => _settings.Current.TrimInputValues;

	public string? Ask(string prompt, bool allowEmpty = false) =>
		_prompt.Ask(prompt, allowEmpty, Trim);

	public string? Ask(string prompt, string defaultValue) =>
		_prompt.Ask(prompt, defaultValue, Trim);

	public string? Secret(string prompt) =>
		_prompt.Secret(prompt, Trim);
}
