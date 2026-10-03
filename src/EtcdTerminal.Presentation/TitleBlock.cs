namespace EtcdTerminal.Presentation;

/// <summary>
/// A section heading, separated from the surrounding blocks by the renderer.
/// </summary>
public sealed record TitleBlock(StyledText Title) : Block;
