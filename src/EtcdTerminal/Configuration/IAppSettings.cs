namespace EtcdTerminal.Configuration;

public interface IAppSettings
{
	int PageSize { get; set; }
	bool TrimInputValues { get; set; }
}