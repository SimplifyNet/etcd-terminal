namespace EtcdTerminal.Infrastructure.Configuration;

internal static class JsonConfigFile
{
	internal static void WriteAllTextAtomic(string path, string contents)
	{
		var tmpPath = path + ".tmp";

		File.WriteAllText(tmpPath, contents);

		UnixFileMode? mode = null;

		if (File.Exists(path) && (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()))
		{
			try
			{
				mode = File.GetUnixFileMode(path);
			}
			catch
			{
				mode = null;
			}
		}

		File.Move(tmpPath, path, overwrite: true);

		if (mode.HasValue && (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()))
		{
			try
			{
				File.SetUnixFileMode(path, mode.Value);
			}
			catch
			{
			}
		}
	}
}
