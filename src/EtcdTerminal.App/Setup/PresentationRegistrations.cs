using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Screens.Permissions;
using EtcdTerminal.App.Screens.Roles;
using EtcdTerminal.App.Screens.Users;
using Simplify.DI;

namespace EtcdTerminal.App.Setup;

public static class PresentationRegistrations
{
	public static IDIRegistrator RegisterEngine(this IDIRegistrator registrator) => registrator
		.Register<Menu>(LifetimeType.Transient)
		.Register<Prompt>(LifetimeType.Transient);

	public static IDIRegistrator RegisterComponents(this IDIRegistrator registrator) => registrator
		.Register<StatusBar>(LifetimeType.Transient)
		.Register<Header>(LifetimeType.Transient)
		.Register<MenuScreen>(LifetimeType.Transient)
		.Register<Screen>(LifetimeType.Transient)
		.Register<UserListLayout>(LifetimeType.Transient)
		.Register<RoleListLayout>(LifetimeType.Transient)
		.Register<PermissionListLayout>(LifetimeType.Transient)
		.Register<BrowseLayout>(LifetimeType.Transient)
		.Register<ListView>(LifetimeType.Transient)
		.Register<ListBrowser>(LifetimeType.Transient)
		.Register<PressAnyKeyPrompt>(LifetimeType.Transient)
		.Register<Message>(LifetimeType.Transient)
		.Register<MultiLinePasteReader>(LifetimeType.Transient)
		.Register<UserInput>(LifetimeType.Transient)
		.Register<CancellableLoad>(LifetimeType.Transient)
		.Register<Spinner>(LifetimeType.Transient);
}
