namespace EtcdTerminal.Presentation;

public sealed record ChoiceList<TId>(string? Title, IReadOnlyList<Choice<TId>> Items, TId? SelectedId = default);
