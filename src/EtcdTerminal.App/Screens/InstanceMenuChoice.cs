using EtcdTerminal.Configuration;

namespace EtcdTerminal.App.Screens;

public sealed record InstanceMenuChoice(InstanceFixedAction? Action, EtcdConnectionConfig? Instance);
