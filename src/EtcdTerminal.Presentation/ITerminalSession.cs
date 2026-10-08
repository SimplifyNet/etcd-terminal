namespace EtcdTerminal.Presentation;

/// <summary>
/// Owns the raw terminal lifecycle for one run: encoding, the alternate
/// screen, the scroll region, the window background and cursor visibility.
/// </summary>
public interface ITerminalSession
{
	void Start();

	void Stop();

	void OnInterrupt(Action handler);

	/// Runs the handler after the window size changed and the viewport was
	/// reserved again for the new size; the screen is expected to redraw.
	void OnResize(Action handler);
}
