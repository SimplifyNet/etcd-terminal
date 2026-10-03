namespace EtcdTerminal.Presentation;

/// <summary>
/// Owns the raw terminal lifecycle for one run: encoding, the alternate
/// screen, the scroll region, the window background and cursor visibility.
/// </summary>
public interface ITerminalSession
{
	void Start();

	void Stop();

	/// <summary>
	/// Widens the scroll region to include the footer row while a frame owns
	/// the console: the frame draws the footer itself, and a full height frame
	/// ends its lines with a feed on the viewport's last row, which would
	/// otherwise scroll the viewport by one line on every paint.
	/// </summary>
	void BeginFrame();

	/// <summary>
	/// Restores the viewport split (rows 1..H-1) once the frame is released,
	/// so streaming output scrolls inside the viewport and never moves the
	/// footer. Changing the region homes the cursor; the caller positions it.
	/// </summary>
	void EndFrame();

	void OnInterrupt(Action handler);
}
