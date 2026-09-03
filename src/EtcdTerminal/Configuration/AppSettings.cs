namespace EtcdTerminal.Configuration;

public class AppSettings : IAppSettings
{
	public int PageSize { get; set; } = 30;
	public bool TrimInputValues { get; set; } = true;
}