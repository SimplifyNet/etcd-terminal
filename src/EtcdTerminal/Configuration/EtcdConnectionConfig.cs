namespace EtcdTerminal.Configuration;

public sealed class EtcdConnectionConfig
{
	public string Name { get; init; } = string.Empty;
	public string ConnectionString { get; init; } = string.Empty;
	public string? Username { get; init; }
	public string? Password { get; init; }
	public bool IsAuthenticationEnabled => !string.IsNullOrEmpty(Username);
}
