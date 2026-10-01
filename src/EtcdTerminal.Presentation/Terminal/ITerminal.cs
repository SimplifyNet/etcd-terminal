namespace EtcdTerminal.Presentation.Terminal;

public interface ITerminal : ITerminalOutput, ITerminalCursor, ITerminalInput, ITerminalStyle, ITerminalWidgets, ITerminalLifecycle
{
}
