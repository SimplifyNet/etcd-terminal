namespace EtcdTerminal.Presentation;

public sealed record Choice<TId>(TId Id, string Label);
