using EtcdTerminal;
using EtcdTerminal.App.Screens;
using EtcdTerminal.App.Setup;
using Simplify.DI;
using Spectre.Console;

const string SetBgCommand = "\x1b]11;#252629\x07";
const string ResetBgCommand = "\x1b]111\x07";
const string ShuttingDown = "[yellow]Shutting down...[/]";
const string PressAnyKeyRestart = "\n[grey]Press any key to restart...[/]";

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.Write(SetBgCommand);

Console.CancelKeyPress += (_, args) =>
{
	args.Cancel = true;
	Console.Write(ResetBgCommand);
	Console.ResetColor();
	Console.WriteLine();
	AnsiConsole.MarkupLine(ShuttingDown);
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
		AnsiConsole.MarkupLine(PressAnyKeyRestart);
		Console.ReadKey(true);
	}
}
