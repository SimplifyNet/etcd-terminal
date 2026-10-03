namespace EtcdTerminal.Presentation;

/// <summary>
/// Columns and cells of tabular content. <see cref="Header"/> is empty for
/// tables that carry no heading; column widths and overflow are the renderer's
/// decision.
/// </summary>
public sealed record TableBlock(IReadOnlyList<StyledText> Header, IReadOnlyList<IReadOnlyList<StyledText>> Rows) : Block;
