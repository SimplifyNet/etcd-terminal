using EtcdTerminal.Configuration;
using EtcdTerminal.Security;

namespace EtcdTerminal.Infrastructure.Configuration;

public sealed class ProtectedConfigRepository(IConnectionConfigRepository _repository, IConfigProtector _protector) : IConnectionConfigRepository
{
	public IReadOnlyList<EtcdConnectionConfig> LoadInstances() =>
		[.. _repository.LoadInstances()
			.Select(Decrypt)
			.Where(c => c is not null)
			.Cast<EtcdConnectionConfig>()];

	public void AddInstance(EtcdConnectionConfig config) =>
		_repository.AddInstance(WithPassword(config, Encrypt(config.Password)));

	public void RemoveInstance(string name) => _repository.RemoveInstance(name);

	public void MoveUp(string name) => _repository.MoveUp(name);

	public void MoveDown(string name) => _repository.MoveDown(name);

	private EtcdConnectionConfig? Decrypt(EtcdConnectionConfig config)
	{
		if (config.Password is null)
			return config;

		try
		{
			return WithPassword(config, _protector.Decrypt(config.Password));
		}
		catch
		{
			return null;
		}
	}

	private static EtcdConnectionConfig WithPassword(EtcdConnectionConfig config, string? password) =>
		new()
		{
			Name = config.Name,
			ConnectionString = config.ConnectionString,
			Username = config.Username,
			Password = password
		};

	private string? Encrypt(string? password) => password is null ? null : _protector.Encrypt(password);
}