using EtcdTerminal.Infrastructure;
using EtcdTerminal.Infrastructure.Security;
using EtcdTerminal.Keys;
using EtcdTerminal.Roles;
using EtcdTerminal.Security;
using EtcdTerminal.Users;
using Simplify.DI;

namespace EtcdTerminal.App.Setup;

public static class DomainRegistrations
{
	public static IDIRegistrator RegisterKeys(this IDIRegistrator registrator) => registrator
		.Register<IEtcdKeyStore, DotnetEtcdKeyStore>(LifetimeType.Singleton)
		.Register<IReadableKeysProvider, ReadableKeysProvider>(LifetimeType.Transient)
		.Register<IKeyImporter, KeyImporter>(LifetimeType.Transient);

	public static IDIRegistrator RegisterUsers(this IDIRegistrator registrator) => registrator
		.Register<IEtcdUserAdmin, DotnetEtcdUserAdmin>(LifetimeType.Singleton);

	public static IDIRegistrator RegisterRoles(this IDIRegistrator registrator) => registrator
		.Register<IEtcdRoleAdmin, DotnetEtcdRoleAdmin>(LifetimeType.Singleton);

	public static IDIRegistrator RegisterSecurity(this IDIRegistrator registrator) => registrator
		.Register<IEtcdAuthAdmin, DotnetEtcdAuthAdmin>(LifetimeType.Singleton)
		.Register<IUserCapabilitiesProvider, UserCapabilitiesProvider>(LifetimeType.Transient)
		.Register<IConfigProtector, ConfigProtector>(LifetimeType.Singleton);
}
