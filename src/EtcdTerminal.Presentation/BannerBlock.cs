namespace EtcdTerminal.Presentation;

/// <summary>
/// The application banner. Only the literal text travels in the model; the
/// widget, its centering and its colour are the renderer's decision.
/// </summary>
public sealed record BannerBlock(string Text) : Block;
