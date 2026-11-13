namespace EtcdTerminal.Clipboard;

/// <summary>
/// Puts text on the clipboard of the machine the user is working at.
/// A platform may have no clipboard tool at all or the terminal may ignore
/// the request, so callers report a false result instead of assuming the
/// copy happened.
/// </summary>
public interface IClipboard
{
	bool Copy(string text);
}
