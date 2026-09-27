namespace EtcdTerminal.Presentation;

/// <summary>
/// Content of a single panel: literal lines with semantic roles and the logical
/// region the panel belongs to. Carries no sizes, padding, markup or callbacks.
/// </summary>
public sealed record PanelModel(IReadOnlyList<PanelLine> Lines, PanelKind Kind = PanelKind.Default);
