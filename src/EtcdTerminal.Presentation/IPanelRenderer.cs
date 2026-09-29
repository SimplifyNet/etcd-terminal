namespace EtcdTerminal.Presentation;

/// <summary>
/// Writes panel content to the console. Implemented by Infrastructure.
/// </summary>
public interface IPanelRenderer
{
	void Write(PanelModel panel);
}
