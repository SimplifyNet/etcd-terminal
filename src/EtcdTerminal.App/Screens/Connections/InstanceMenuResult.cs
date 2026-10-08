using EtcdTerminal.Configuration;

namespace EtcdTerminal.App.Screens.Connections;

public sealed record InstanceMenuResult(IReadOnlyList<EtcdConnectionConfig> Instances, InstanceMenuChoice? Choice);
