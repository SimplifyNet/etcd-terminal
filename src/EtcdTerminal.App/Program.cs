using EtcdTerminal.App.Components;
using EtcdTerminal.App.Screens;
using EtcdTerminal.App.Setup;
using EtcdTerminal.Configuration;
using Simplify.DI;
using Spectre.Console;

const string PressAnyKeyRestart = "\n[grey]Press any key to restart...[/]";

DIContainer.Current
	.RegisterAll()
	.Verify();

Console.OutputEncoding = System.Text.Encoding.UTF8;
TerminalPanel.SetDarkBackground();

static void Cleanup()
{
	TerminalPanel.ClearScreen();
	TerminalPanel.ResetBackground();
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

			settingsRepository.Load();

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
