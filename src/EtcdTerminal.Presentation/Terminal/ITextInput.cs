namespace EtcdTerminal.Terminal;

public interface ITextInput
{
	/// Returns null when the user cancels (Escape).
	string? ReadLine(string prompt, string? defaultValue = null);

	/// Returns null when the user cancels (Escape).
	string? ReadSecret(string prompt);
}
