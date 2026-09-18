namespace EtcdTerminal.Terminal;

public interface ITerminalWidgets
{
	void WriteTable(TableData table);

	void WriteBanner(string text);

	void WriteException(Exception ex);
}
