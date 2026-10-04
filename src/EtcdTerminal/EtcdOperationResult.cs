namespace EtcdTerminal;

public readonly record struct EtcdOperationResult(bool Success, string? ErrorMessage, EtcdOperationFailureKind? Kind)
{
	public static EtcdOperationResult Ok() => new(true, null, null);

	public static EtcdOperationResult Fail(string message, EtcdOperationFailureKind kind) => new(false, message, kind);
}
