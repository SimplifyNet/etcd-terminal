namespace EtcdTerminal.Configuration;

public interface IAppSettings
{
	int PageSize { get; }
	bool TrimInputValues { get; }
}