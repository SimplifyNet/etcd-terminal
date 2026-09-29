namespace EtcdTerminal.Terminal;

public interface ITerminal : ITerminalOutput, ITerminalCursor, ITerminalInput, ITerminalStyle, ITerminalWidgets, ITerminalLifecycle
{
}
