namespace EtcdTerminal;

public sealed class EtcdOperationException(EtcdOperationFailureKind kind, string message, Exception? inner = null) : Exception(message, inner)
{
	public EtcdOperationFailureKind Kind { get; } = kind;
}
