namespace EtcdTerminal.Presentation;

public interface ISelectionPrompt
{
	Choice<TId>? Select<TId>(ChoiceList<TId> list);
}
