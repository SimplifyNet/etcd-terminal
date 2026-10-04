using Grpc.Core;

namespace EtcdTerminal.Infrastructure;

public static class GrpcErrorTranslator
{
	public static bool IsCancellation(RpcException ex) =>
		ex.StatusCode == StatusCode.Cancelled;

	public static EtcdOperationException Translate(RpcException ex) => ex.StatusCode switch
	{
		StatusCode.PermissionDenied => AccessDenied(ex),
		StatusCode.Unauthenticated => AccessDenied(ex),
		StatusCode.Unavailable => Failure(EtcdOperationFailureKind.Unavailable, ex),
		StatusCode.DeadlineExceeded => Failure(EtcdOperationFailureKind.Unconfirmed, ex),
		StatusCode.InvalidArgument when IsAuthenticationFailure(ex) => AccessDenied(ex),
		_ => Failure(EtcdOperationFailureKind.TransportError, ex)
	};

	public static EtcdOperationException Failure(EtcdOperationFailureKind kind, RpcException ex) =>
		new(kind, $"etcd operation failed ({ex.StatusCode}): {DetailOrMessage(ex)}", ex);

	public static EtcdOperationResult RpcFail(RpcException ex) =>
		EtcdOperationResult.Fail(DetailOrMessage(ex), Translate(ex).Kind);

	// Verified against etcd 3.7.1: missing users and roles surface as
	// FailedPrecondition with a not-found detail, not as gRPC NotFound.
	public static bool IsMissingUser(RpcException ex) =>
		ex.StatusCode == StatusCode.FailedPrecondition && ex.Status.Detail.Contains("user name not found", StringComparison.Ordinal);

	public static bool IsMissingRole(RpcException ex) =>
		ex.StatusCode == StatusCode.FailedPrecondition && ex.Status.Detail.Contains("role name not found", StringComparison.Ordinal);

	private static EtcdOperationException AccessDenied(RpcException ex) =>
		Failure(EtcdOperationFailureKind.AccessDenied, ex);

	private static string DetailOrMessage(RpcException ex) =>
		string.IsNullOrEmpty(ex.Status.Detail) ? ex.Message : ex.Status.Detail;

	private static bool IsAuthenticationFailure(RpcException ex) =>
		ex.Status.Detail.Contains("authentication failed", StringComparison.Ordinal);
}
