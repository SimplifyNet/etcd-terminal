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

static void Cleanup()
{
	var terminal = DIContainer.Current.Resolve<ITerminal>();

	terminal.ClearScreen();
	terminal.ResetBackground();
	terminal.Flush();

	DIContainer.Current.Dispose();
}

Console.CancelKeyPress += (_, args) =>
{
	args.Cancel = true;
	Cleanup();
	Environment.Exit(0);
};

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

			await mainScreen.ShowAsync(config);
		}
		catch (Exception ex)
		{
			var terminal = DIContainer.Current.Resolve<ITerminal>();

			terminal.WriteException(ex);
			terminal.WriteMarkupLine($"[grey]{LocalizationStore.Current.PressAnyKeyRestart}[/]");
			terminal.ReadKey();
		}
	}
}
finally
{
	Cleanup();
}
