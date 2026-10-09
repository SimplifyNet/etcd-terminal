namespace EtcdTerminal.Presentation;

public interface ISelectionPrompt
{
	Choice<TId>? Select<TId>(ChoiceList<TId> list, Action<Choice<TId>>? onHighlight = null);
}
