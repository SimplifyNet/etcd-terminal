namespace EtcdTerminal.Configuration;

public sealed record AppSettings : IAppSettings
{
	public int PageSize { get; init; } = 30;
	public bool TrimInputValues { get; init; } = true;
}