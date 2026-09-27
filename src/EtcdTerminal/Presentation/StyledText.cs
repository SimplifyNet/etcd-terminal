namespace EtcdTerminal.Presentation;

/// <summary>
/// A literal text run with its semantic role. Text is never markup.
/// </summary>
public sealed record StyledText(string Text, TextRole Role = TextRole.Default);
