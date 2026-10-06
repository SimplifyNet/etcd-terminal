using Simplify.DI;

namespace EtcdTerminal.App.Setup;

public static class IocRegistrations
{
	public static IDIContainerProvider RegisterAll(this IDIContainerProvider provider)
	{
		provider.RegisterTerminal()
				.RegisterTheming()
				.RegisterLocalization()
				.RegisterConfiguration()
				.RegisterSession()
				.RegisterClient()
				.RegisterKeys()
				.RegisterUsers()
				.RegisterRoles()
				.RegisterSecurity()
				.RegisterEnvironment()
				.RegisterApplication()
				.RegisterEngine()
				.RegisterComponents()
				.RegisterScreens();

		return provider;
	}
}
