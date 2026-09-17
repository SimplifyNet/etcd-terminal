namespace EtcdTerminal.Keys;

public readonly record struct KeyImportResult(int Created, int Overwritten, int Failed);
