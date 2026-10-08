namespace EtcdTerminal.Presentation;

public sealed record ActionPanelBlock(IReadOnlyList<StyledText> Title, IReadOnlyList<StyledText> Actions) : Block;
