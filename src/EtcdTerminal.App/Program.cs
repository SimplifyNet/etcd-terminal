using EtcdTerminal.App.Screens;
using EtcdTerminal.App.Setup;
using EtcdTerminal.Configuration;
using EtcdTerminal.Terminal;
using EtcdTerminal.Localization;
using Simplify.DI;

DIContainer.Current
	.RegisterAll()
	.Verify();

DIContainer.Current
	.Resolve<ITerminal>()
	.Initialize();

var cleanedUp = 0;

void Cleanup()
{
	if (Interlocked.Exchange(ref cleanedUp, 1) is not 0)
		return;

	var terminal = DIContainer.Current.Resolve<ITerminal>();

	terminal.Clear();
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
			var settingsStore = scope.Resolver.Resolve<IAppSettingsStore>();

			settingsStore.Update(settingsRepository.Load());

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
			var localization = DIContainer.Current.Resolve<ILocalization>();

			terminal.WriteException(ex);
			terminal.WriteIndentedLine(localization.PressAnyKeyRestart, TerminalColor.Muted);
			terminal.ReadKey();
		}
	}
}
finally
{
	Cleanup();
}
