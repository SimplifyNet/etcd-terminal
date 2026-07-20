using EtcdTerminal.App.Screens;
using EtcdTerminal.App.Setup;
using Simplify.DI;
using Spectre.Console;

const string SetBgCommand = "\x1b]11;#0a0a0a\x07";
const string ResetBgCommand = "\x1b]111\x07";
const string PressAnyKeyRestart = "\n[grey]Press any key to restart...[/]";

DIContainer.Current
	.RegisterAll()
	.Verify();

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.Write(SetBgCommand);

static void Cleanup()
{
	Console.Write("\x1b[2J\x1b[H");
	Console.Write(ResetBgCommand);
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
