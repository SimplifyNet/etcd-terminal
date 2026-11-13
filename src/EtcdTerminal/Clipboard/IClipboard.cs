namespace EtcdTerminal.Clipboard;

/// <summary>
/// Puts text on the clipboard of the terminal the application runs in.
/// A terminal may refuse or ignore the request, so callers report a false
/// result instead of assuming the copy happened.
/// </summary>
public interface IClipboard
{
	bool Copy(string text);
}
