namespace EtcdTerminal.Presentation;

/// <summary>
/// Shows an animated indicator for the duration of one long operation.
/// Implemented by Infrastructure, which owns the animation, the cursor and the
/// region the indicator occupies. The caller owns the work, the cancellation
/// policy and the surrounding screen, so the indicator must never be started
/// while a screen frame is live.
/// </summary>
public interface IStatusIndicator
{
	/// <summary>
	/// Runs <paramref name="action"/> to completion while the indicator is
	/// showing, then removes the indicator. The message stays literal; the role
	/// selects the style of the indicator.
	/// </summary>
	Task RunAsync(StyledText message, Func<Task> action);
}
