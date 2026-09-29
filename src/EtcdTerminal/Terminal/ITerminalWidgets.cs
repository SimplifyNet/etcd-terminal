namespace EtcdTerminal.Terminal;

public interface ITerminalWidgets
{
	void WriteTable(TableData table);

	void WriteException(Exception ex);
}
