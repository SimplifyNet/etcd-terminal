using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Theming;
using EtcdTerminal.Infrastructure.Terminal;
using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation.Theming;
using Simplify.DI;
using Spectre.Console;

namespace EtcdTerminal.App.Setup;

public static class TerminalRegistrations
{
	public static IDIRegistrator RegisterTerminal(this IDIRegistrator registrator) => registrator
		.Register<ITextInput, SpectreTextInput>(LifetimeType.Singleton)
		.Register<ITerminalSession, ConsoleTerminalSession>(LifetimeType.Singleton)
		.Register<IScreenCanvas, SpectreScreenCanvas>(LifetimeType.Singleton)
		.Register<IKeyReader, SpectreKeyReader>(LifetimeType.Singleton)
		.Register<ISelectionPrompt, SpectreSelectionPrompt>(LifetimeType.Singleton)
		.Register<ILiveFrame, SpectreLiveFrame>(LifetimeType.Singleton)
		.Register(c => SpectreConsoleHost.Default, LifetimeType.Singleton)
		.Register<EscapableConsole>(c => new(c.Resolve<IAnsiConsole>()), LifetimeType.Singleton)
		.Register<RoleStyleMapper>(LifetimeType.Singleton)
		.Register<BlockRenderer>(LifetimeType.Singleton)
		.Register<StatusBarRenderer>(LifetimeType.Singleton)
		.Register<IStatusIndicator, SpectreStatusIndicator>(LifetimeType.Singleton);

	public static IDIRegistrator RegisterTheming(this IDIRegistrator registrator) => registrator
		.Register<IThemeCatalog, ThemeCatalog>(LifetimeType.Singleton);

	public static IDIRegistrator RegisterLocalization(this IDIRegistrator registrator) => registrator
		.Register<ILocalizationCatalog, LocalizationCatalog>(LifetimeType.Singleton);
}
