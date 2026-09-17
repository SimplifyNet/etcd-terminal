namespace EtcdTerminal.Permissions;

/// <summary>
/// How wide an etcd permission is: a single key, everything under a prefix, or an explicit range.
/// </summary>
public enum PermissionScope
{
	Key,
	Prefix,
	Range
}
