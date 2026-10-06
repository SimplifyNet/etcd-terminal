using EtcdTerminal.App;
using EtcdTerminal.App.Setup;
using EtcdTerminal.Presentation;
using Simplify.DI;

DIContainer.Current
	.RegisterAll()
	.Verify();

var runner = DIContainer.Current.Resolve<AppRunner>();

runner.Start();

var shutdown = 0;

void Shutdown()
{
	if (Interlocked.Exchange(ref shutdown, 1) is not 0)
		return;

	runner.Stop();
	DIContainer.Current.Dispose();
}

DIContainer.Current
	.Resolve<ITerminalSession>()
	.OnInterrupt(() =>
	{
		Shutdown();
		Environment.Exit(0);
	});

try
{
	await runner.RunAsync();
}
finally
{
	Shutdown();
}
