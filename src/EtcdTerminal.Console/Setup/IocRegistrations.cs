using EtcdTerminal.Console.Screens;
using EtcdTerminal.Infrastructure.Configuration;
using EtcdTerminal.Infrastructure.Etcd;
using EtcdTerminal;
using Simplify.DI;

namespace EtcdTerminal.Console.DI;

public static class IocRegistrations
{
    public static IDIContainerProvider RegisterAll(this IDIContainerProvider provider)
    {
        provider.RegisterConfiguration()
               .RegisterIEtcdClient()
               .RegisterScreens()
               .Verify();

        return provider;
    }

    private static IDIContainerProvider RegisterConfiguration(this IDIContainerProvider provider)
    {
        provider.Register<IConnectionConfigRepository, ConnectionConfigRepository>(LifetimeType.Singleton);
        return provider;
    }

    private static IDIContainerProvider RegisterIEtcdClient(this IDIContainerProvider provider)
    {
        provider.Register<IEtcdClient, EtcdClientAdapter>(LifetimeType.Singleton);
        return provider;
    }

    private static IDIContainerProvider RegisterScreens(this IDIContainerProvider provider)
    {
        provider.Register<InstanceSelectionScreen>(LifetimeType.Transient);
        provider.Register<MainScreen>(LifetimeType.Transient);
        provider.Register<KeyBrowserScreen>(LifetimeType.Transient);
        provider.Register<KeySearchScreen>(LifetimeType.Transient);
        provider.Register<KeyCreateScreen>(LifetimeType.Transient);
        provider.Register<KeyEditScreen>(LifetimeType.Transient);
        provider.Register<UserManagementScreen>(LifetimeType.Transient);
        provider.Register<RoleManagementScreen>(LifetimeType.Transient);
        provider.Register<PermissionViewScreen>(LifetimeType.Transient);
        return provider;
    }
}
