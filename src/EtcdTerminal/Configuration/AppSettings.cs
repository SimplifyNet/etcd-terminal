namespace EtcdTerminal.Configuration;

public sealed record AppSettings
{
	public int PageSize { get; init; } = 30;
	public bool TrimInputValues { get; init; } = true;
}