using EtcdTerminal.Configuration;

namespace EtcdTerminal.App.Screens.Connections;

public sealed record InstanceMenuChoice(InstanceFixedAction? Action, EtcdConnectionConfig? Instance);
