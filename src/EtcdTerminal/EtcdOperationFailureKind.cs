namespace EtcdTerminal;

/// <summary>
/// How an etcd operation failed. Absence is reported as null, an empty
/// collection, or false — never as a failure. Cancellation is reported as
/// <see cref="OperationCanceledException"/>, never as a failure.
/// </summary>
public enum EtcdOperationFailureKind
{
	/// <summary>
	/// The server refused the operation: permission denied or invalid authentication.
	/// </summary>
	AccessDenied,

	/// <summary>
	/// The server could not be reached.
	/// </summary>
	Unavailable,

	/// <summary>
	/// The operation timed out and may have committed on the server.
	/// </summary>
	Unconfirmed,

	/// <summary>
	/// Any other transport or server failure.
	/// </summary>
	TransportError
}
