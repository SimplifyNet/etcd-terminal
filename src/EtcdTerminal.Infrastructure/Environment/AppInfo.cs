using System.Reflection;
using EtcdTerminal.Environment;

namespace EtcdTerminal.Infrastructure.Environment;

public sealed class AppInfo : IAppInfo
{
	public string Version => GetVersion();

	private static string GetVersion()
	{
		var version = Assembly.GetEntryAssembly()?.GetName().Version;

		if (version is null)
			return "0.0";

		return $"{version.Major}.{version.Minor}" + (version.Build != 0 ? "." + version.Build : "");
	}
}
