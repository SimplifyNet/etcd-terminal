using EtcdTerminal.Console.Screens;
using EtcdTerminal.Console.Setup;
using Simplify.DI;
using Spectre.Console;

DIContainer.Current.RegisterAll()
	.Verify();

while (true)
{
	try
	{
		using var scope = DIContainer.Current.BeginLifetimeScope();

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
		AnsiConsole.MarkupLine("\n[grey]Press any key to restart...[/]");
		Console.ReadKey(true);
	}
}
