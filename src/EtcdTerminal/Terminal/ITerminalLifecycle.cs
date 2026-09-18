namespace EtcdTerminal.Terminal;

public interface ITerminalLifecycle
{
	void Initialize();

	void OnInterrupt(Action handler);
}
