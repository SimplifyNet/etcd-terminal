namespace EtcdTerminal.Presentation;

/// <summary>
/// Writes the footer for the current session and owns the rows it reserves.
/// Implemented by Infrastructure.
/// </summary>
public interface IStatusBarRenderer
{
	void Write(StatusBarModel model);

	/// <summary>
	/// Keeps <paramref name="rows"/> free rows above the pinned footer while no
	/// frame owns the screen: a cursor that would collide with the footer is
	/// scrolled clear and parked on the first free row. How many rows the
	/// footer reserves and where the cursor goes is the renderer's decision,
	/// not the caller's.
	/// </summary>
	void EnsureRoomAbove(int rows);

	/// <summary>
	/// Erases everything from the cursor to the bottom of the screen. Output
	/// that streams under a released frame would otherwise land on rows that
	/// frame painted earlier, and the blank lines of the new text pass over them
	/// without removing them. The footer lives on those rows too, so a caller
	/// that keeps writing is responsible for drawing it again.
	/// </summary>
	void ClearBelow();
}
