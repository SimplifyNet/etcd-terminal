using System.Text;
using Etcdserverpb;
using EtcdTerminal.Keys;
using EtcdTerminal.Permissions;
using Google.Protobuf;

namespace EtcdTerminal.Infrastructure;

public sealed class DotnetEtcdKeyStore(EtcdConnectionHandle _handle) : IEtcdKeyStore
{
	private dotnet_etcd.interfaces.IEtcdClient Client => _handle.Client;

	public Task<EtcdKeyValue?> GetKeyAsync(string key, CancellationToken ct = default) =>
		_handle.Guard<EtcdKeyValue?>(async () =>
		{
			var response = await Client.GetAsync(key, cancellationToken: ct);

			if (response.Kvs.Count == 0)
				return null;

			return EtcdProtoMapper.MapKeyValue(response.Kvs[0]);
		});

	public Task<IReadOnlyList<EtcdKeyValue>> GetKeysByPrefixAsync(string prefix, CancellationToken ct = default) =>
		_handle.Guard<IReadOnlyList<EtcdKeyValue>>(async () =>
		{
			// The vendor prefix helper is not byte-exact (e.g. for a prefix ending in
			// U+D7FF), so the [prefix, successor) bounds are built here explicitly.
			// An empty prefix reads all keys as [zero-byte, zero-byte).
			var start = prefix.Length == 0 || prefix == PermissionRange.AllKeys
				? [0]
				: Encoding.UTF8.GetBytes(prefix);

			var response = await Client.GetAsync(new RangeRequest
			{
				Key = ByteString.CopyFrom(start),
				RangeEnd = ByteString.CopyFrom(PermissionRange.PrefixRangeEndBytes(start))
			}, cancellationToken: ct);

			return [.. response.Kvs.Select(EtcdProtoMapper.MapKeyValue)];
		});

	public Task<IReadOnlyList<EtcdKeyValue>> GetKeysByRangeAsync(string start, string endExclusive, CancellationToken ct = default)
	{
		if (endExclusive.Length == 0)
			throw new ArgumentException("A range read requires an explicit exclusive end.", nameof(endExclusive));

		return _handle.Guard<IReadOnlyList<EtcdKeyValue>>(async () =>
		{
			var response = await Client.GetAsync(new RangeRequest
			{
				Key = ByteString.CopyFromUtf8(start),
				RangeEnd = endExclusive == PermissionRange.AllKeys
					? ByteString.CopyFrom(0x00)
					: ByteString.CopyFromUtf8(endExclusive)
			}, cancellationToken: ct);

			return [.. response.Kvs.Select(EtcdProtoMapper.MapKeyValue)];
		});
	}

	public Task<bool> CreateKeyAsync(string key, string value, CancellationToken ct = default) =>
		_handle.Guard<bool>(async () =>
		{
			var response = await Client.TransactionAsync(new TxnRequest
			{
				Compare = { EtcdProtoMapper.VersionIs(key, 0, Compare.Types.CompareResult.Equal) },
				Success = { EtcdProtoMapper.PutValue(key, value, ignoreLease: false) }
			}, cancellationToken: ct);

			return response.Succeeded;
		});

	public Task<bool> UpdateKeyAsync(string key, string value, CancellationToken ct = default) =>
		_handle.Guard<bool>(async () =>
		{
			var response = await Client.TransactionAsync(new TxnRequest
			{
				Compare = { EtcdProtoMapper.VersionIs(key, 0, Compare.Types.CompareResult.Greater) },
				Success = { EtcdProtoMapper.PutValue(key, value, ignoreLease: true) }
			}, cancellationToken: ct);

			return response.Succeeded;
		});

	public Task<bool> DeleteKeyAsync(string key, CancellationToken ct = default) =>
		_handle.Guard<bool>(async () =>
		{
			var response = await Client.DeleteAsync(key, cancellationToken: ct);

			return response.Deleted > 0;
		});
}
