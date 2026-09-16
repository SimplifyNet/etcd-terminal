namespace EtcdTerminal.Terminal;

public sealed record MenuItem<TId>(TId Id, string Label, bool IsSelectable = true);
