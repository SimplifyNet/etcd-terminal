using EtcdTerminal.App.Screens;
using EtcdTerminal.App.Setup;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Theming;
using EtcdTerminal.Configuration;
using EtcdTerminal.Terminal;
using EtcdTerminal.Theming;
using EtcdTerminal.Localization;
using Simplify.DI;

DIContainer.Current
	.RegisterAll()
	.Verify();

ThemeStore.Current = new ReddyTheme();
LocalizationStore.Current = new EnglishLocalization();

DIContainer.Current
	.Resolve<ITerminal>()
	.Initialize();

var cleanedUp = 0;

void Cleanup()
{
	if (Interlocked.Exchange(ref cleanedUp, 1) is not 0)
		return;

	var terminal = DIContainer.Current.Resolve<ITerminal>();

	terminal.ClearScreen();
	terminal.ResetBackground();
	terminal.SetCursorVisible(true);
	terminal.Flush();

	DIContainer.Current.Dispose();
}

DIContainer.Current
	.Resolve<ITerminal>()
	.OnInterrupt(() =>
	{
		Cleanup();
		Environment.Exit(0);
	});

try
{
	while (true)
	{
		try
		{
			using var scope = DIContainer.Current.BeginLifetimeScope();
			var settingsRepository = scope.Resolver.Resolve<IAppSettingsRepository>();

			AppSettingsStore.Current = settingsRepository.Load();

			var instanceScreen = scope.Resolver.Resolve<InstanceSelectionScreen>();
			var config = await instanceScreen.ShowAsync();

			if (config is null)
				return;

			var mainScreen = scope.Resolver.Resolve<MainScreen>();

			await mainScreen.ShowAsync();
		}
		catch (Exception ex)
		{
			var terminal = DIContainer.Current.Resolve<ITerminal>();

			terminal.WriteException(ex);
			terminal.WriteIndentedLine(LocalizationStore.Current.PressAnyKeyRestart, TerminalColor.Muted);
			terminal.ReadKey();
		}
	}
}
finally
{
	Cleanup();
}
