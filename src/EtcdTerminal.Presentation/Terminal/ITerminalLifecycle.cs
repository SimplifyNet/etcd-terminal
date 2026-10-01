namespace EtcdTerminal.Presentation.Terminal;

public interface ITerminalLifecycle
{
	void Initialize();

	void Shutdown();

	void OnInterrupt(Action handler);
}
