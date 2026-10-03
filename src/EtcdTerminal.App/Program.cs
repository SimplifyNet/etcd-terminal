using EtcdTerminal.App;
using EtcdTerminal.App.Setup;
using EtcdTerminal.Presentation;
using Simplify.DI;

DIContainer.Current
	.RegisterAll()
	.Verify();

var runner = DIContainer.Current.Resolve<AppRunner>();

runner.Start();

DIContainer.Current
	.Resolve<ITerminalSession>()
	.OnInterrupt(() =>
	{
		runner.Stop();
		Environment.Exit(0);
	});

try
{
	await runner.RunAsync();
}
finally
{
	runner.Stop();
}
