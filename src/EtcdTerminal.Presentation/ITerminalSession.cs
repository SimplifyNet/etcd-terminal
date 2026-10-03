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
}
