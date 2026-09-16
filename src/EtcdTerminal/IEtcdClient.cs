using EtcdTerminal.Configuration;
using EtcdTerminal.Keys;
using EtcdTerminal.Roles;
using EtcdTerminal.Security;
using EtcdTerminal.Users;

namespace EtcdTerminal;

public interface IEtcdClient : IEtcdConnection, IEtcdKeyStore, IEtcdUserAdmin, IEtcdRoleAdmin, IEtcdAuthAdmin, IDisposable
{
}
