namespace EtcdTerminal.Terminal;

public sealed record TableData(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<string>> Rows)
{
	public string? Title { get; init; }
}
