namespace EtcdTerminal.App.Components;

/// <summary>One action of a sub-menu. The owning screen keeps the menu
/// title, labels and order; the command only performs the action.</summary>
public interface IMenuCommand<out TAction> where TAction : struct, Enum
{
	TAction Action { get; }

	Task ExecuteAsync();
}
