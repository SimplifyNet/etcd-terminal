namespace EtcdTerminal.Presentation;

/// <summary>
/// Columns and cells of tabular content. <see cref="Header"/> is empty for
/// tables that carry no heading; column widths and overflow are the renderer's
/// decision. <see cref="IsFramed"/> selects the standard framed table style
/// for data lists; browsing tables stay borderless. <see cref="Pointer"/> marks
/// the first column as the row's selection pointer, which hangs on the margin
/// so every row keeps the standard text column.
/// </summary>
public sealed record TableBlock(IReadOnlyList<StyledText> Header, IReadOnlyList<IReadOnlyList<StyledText>> Rows) : Block
{
	public bool IsFramed { get; init; }

	public bool Pointer { get; init; }
}
