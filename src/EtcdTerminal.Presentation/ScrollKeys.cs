namespace EtcdTerminal.Presentation;

/// <summary>
/// Maps the keys that carry a scroll to a <see cref="ScrollStep"/>. A
/// terminal in alternate scroll mode (DECSET 1007) reports the mouse wheel
/// as exactly these keys, so a component that waits for "any key" must
/// recognize them and scroll instead of continuing.
/// </summary>
public static class ScrollKeys
{
	public static ScrollStep? Step(ConsoleKeyInfo key) =>
		key.Key switch
		{
			ConsoleKey.UpArrow => ScrollStep.LineUp,
			ConsoleKey.DownArrow => ScrollStep.LineDown,
			ConsoleKey.PageUp => ScrollStep.PageUp,
			ConsoleKey.PageDown => ScrollStep.PageDown,
			ConsoleKey.Home => ScrollStep.Top,
			ConsoleKey.End => ScrollStep.Bottom,
			_ => null
		};
}
