namespace EtcdTerminal.Presentation;

/// <summary>
/// Owns the console for the duration of one screen: it starts a frame, replaces
/// that frame on every update, and releases the console on end. Implemented by
/// Infrastructure, which is the only layer allowed to place the frame.
/// </summary>
public interface IScreenHost
{
	/// <summary>
	/// Takes the console and draws the first frame.
	/// </summary>
	void Begin(ScreenModel model);

	/// <summary>
	/// Replaces the current frame. Never appends to it.
	/// </summary>
	void Update(ScreenModel model);

	/// <summary>
	/// Releases the console. Must be safe to call after cancellation or failure,
	/// and safe to call without a preceding <see cref="Begin"/>.
	/// </summary>
	void End();
}
