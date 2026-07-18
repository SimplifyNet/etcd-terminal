using EtcdTerminal.Console.Screens;
using EtcdTerminal.Console.Setup;
using Simplify.DI;
using Spectre.Console;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.Write("\x1b]11;#252629\x07");

Console.CancelKeyPress += (_, args) =>
{
	args.Cancel = true;
	Console.Write("\x1b]111\x07");
	Console.ResetColor();
	Console.WriteLine();
	AnsiConsole.MarkupLine("[yellow]Shutting down...[/]");
	Environment.Exit(0);
};

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
