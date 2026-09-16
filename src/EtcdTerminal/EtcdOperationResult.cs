namespace EtcdTerminal;

public readonly record struct EtcdOperationResult(bool Success, string? ErrorMessage)
{
	public static EtcdOperationResult Ok() => new(true, null);

	public static EtcdOperationResult Fail(string errorMessage) => new(false, errorMessage);
}
