using Authpb;
using Etcdserverpb;
using EtcdTerminal.Keys;
using EtcdTerminal.Permissions;
using Google.Protobuf;
using Mvccpb;

namespace EtcdTerminal.Infrastructure;

public static class EtcdProtoMapper
{
	public static Compare VersionIs(string key, long version, Compare.Types.CompareResult result) => new()
	{
		Result = result,
		Target = Compare.Types.CompareTarget.Version,
		Key = ByteString.CopyFromUtf8(key),
		Version = version
	};

	public static RequestOp PutValue(string key, string value, bool ignoreLease) => new()
	{
		RequestPut = new PutRequest
		{
			Key = ByteString.CopyFromUtf8(key),
			Value = ByteString.CopyFromUtf8(value),
			IgnoreLease = ignoreLease
		}
	};

	public static EtcdKeyValue MapKeyValue(KeyValue kv) => new()
	{
		Key = kv.Key.ToStringUtf8(),
		Value = kv.Value.ToStringUtf8(),
		Version = kv.Version,
		CreateRevision = kv.CreateRevision,
		ModRevision = kv.ModRevision,
		Lease = kv.Lease
	};

	// Text bounds (including the zero-byte sentinel) round-trip through UTF-8 exactly.
	// Non-text server bounds decode with replacement characters: display-safe and
	// self-consistent, but not byte-faithful. There is no binary key editor.
	public static EtcdPermission MapPermission(Permission perm) => new()
	{
		Type = perm.PermType switch
		{
			Permission.Types.Type.Read => PermissionType.Read,
			Permission.Types.Type.Write => PermissionType.Write,
			Permission.Types.Type.Readwrite => PermissionType.ReadWrite,
			_ => PermissionType.Read
		},
		KeyPrefix = perm.Key.ToStringUtf8(),
		RangeEnd = perm.RangeEnd.ToStringUtf8()
	};

	public static Permission.Types.Type MapPermissionType(PermissionType type) => type switch
	{
		PermissionType.Read => Permission.Types.Type.Read,
		PermissionType.Write => Permission.Types.Type.Write,
		PermissionType.ReadWrite => Permission.Types.Type.Readwrite,
		_ => Permission.Types.Type.Read
	};
}
