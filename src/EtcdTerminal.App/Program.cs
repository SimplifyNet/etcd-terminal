using EtcdTerminal.App.Screens;
using EtcdTerminal.App.Setup;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Theming;
using EtcdTerminal.Configuration;
using EtcdTerminal.Terminal;
using EtcdTerminal.Theming;
using EtcdTerminal.Localization;
using Simplify.DI;
using Spectre.Console;

const string PressAnyKeyRestart = "\n[grey]Press any key to restart...[/]";

DIContainer.Current
	.RegisterAll()
	.Verify();

ThemeStore.Current = new ReddyTheme();
LocalizationStore.Current = new EnglishLocalization();

Console.OutputEncoding = System.Text.Encoding.UTF8;

using (var scope = DIContainer.Current.BeginLifetimeScope())
{
	var terminal = scope.Resolver.Resolve<ITerminal>();
	terminal.SetDarkBackground();
}

static void Cleanup()
{
	using var scope = DIContainer.Current.BeginLifetimeScope();
	var terminal = scope.Resolver.Resolve<ITerminal>();
	terminal.ClearScreen();
	terminal.ResetBackground();
	Console.ResetColor();
	Console.Out.Flush();
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
			AnsiConsole.WriteException(ex);
			AnsiConsole.MarkupLine(PressAnyKeyRestart);
			Console.ReadKey(true);
		}
	}
}
finally
{
	Cleanup();
}
