namespace EtcdTerminal.Presentation;

/// <summary>
/// Writes the footer for the current session. Implemented by Infrastructure.
/// </summary>
public interface IStatusBarRenderer
{
	void Write(StatusBarModel model);
}
