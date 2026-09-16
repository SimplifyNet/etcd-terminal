namespace EtcdTerminal.Infrastructure.IO;

/// <summary>
/// File-system helpers for directories and files that must be accessible by the current user only.
/// On Linux/macOS files are created with 0600 and directories with 0700.
/// On Windows the default inherited ACL of the user profile is relied upon.
/// </summary>
internal static class PrivateFileSystem
{
	private const UnixFileMode OwnerOnlyFile = UnixFileMode.UserRead | UnixFileMode.UserWrite;
	private const UnixFileMode OwnerOnlyDirectory = OwnerOnlyFile | UnixFileMode.UserExecute;

	public static void CreateDirectory(string path)
	{
		if (OperatingSystem.IsWindows())
			Directory.CreateDirectory(path);
		else
			Directory.CreateDirectory(path, OwnerOnlyDirectory);
	}

	public static void WriteAllBytes(string path, byte[] bytes)
	{
		using var stream = Create(path, FileMode.CreateNew);

		stream.Write(bytes);
	}

	public static void WriteAllTextAtomic(string path, string contents)
	{
		var tmpPath = path + ".tmp";

		using (var stream = Create(tmpPath, FileMode.Create))
		using (var writer = new StreamWriter(stream))
			writer.Write(contents);

		File.Move(tmpPath, path, overwrite: true);
	}

	private static FileStream Create(string path, FileMode mode)
	{
		var options = new FileStreamOptions
		{
			Mode = mode,
			Access = FileAccess.Write,
			Share = FileShare.None
		};

		if (!OperatingSystem.IsWindows())
			options.UnixCreateMode = OwnerOnlyFile;

		return new FileStream(path, options);
	}
}
