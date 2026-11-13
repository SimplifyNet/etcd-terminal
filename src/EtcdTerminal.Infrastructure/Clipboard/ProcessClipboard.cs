using System.Diagnostics;
using System.Text;
using EtcdTerminal.Clipboard;

namespace EtcdTerminal.Infrastructure.Clipboard;

/// <summary>
/// Copies through the clipboard tool of the current platform: <c>clip</c> on
/// Windows, <c>pbcopy</c> on macOS, <c>wl-copy</c>/<c>xclip</c>/<c>xsel</c>
/// on Linux. The text is piped to standard input, so no shell interprets it.
/// When no tool is available or every tool fails, the OSC 52 fallback is
/// tried, which is the only path that reaches the clipboard over SSH.
/// </summary>
public sealed class ProcessClipboard(IClipboard _fallback) : IClipboard
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

	public bool Copy(string text)
	{
		foreach (var (fileName, arguments) in Candidates())
			if (TryCopy(fileName, arguments, text))
				return true;

		return _fallback.Copy(text);
	}

	private static IEnumerable<(string FileName, string Arguments)> Candidates()
	{
		if (OperatingSystem.IsWindows())
		{
			yield return ("clip", "");

			yield break;
		}

		if (OperatingSystem.IsMacOS())
		{
			yield return ("pbcopy", "");

			yield break;
		}

		if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))
			yield return ("wl-copy", "--type text/plain");

		if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("DISPLAY")))
		{
			yield return ("xclip", "-selection clipboard");
			yield return ("xsel", "--clipboard --input");
		}
	}

	private static bool TryCopy(string fileName, string arguments, string text)
	{
		try
		{
			using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
			{
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				StandardInputEncoding = new UTF8Encoding(false)
			});

			if (process is null)
				return false;

			process.StandardInput.Write(text);
			process.StandardInput.Close();

			if (!process.WaitForExit(Timeout))
			{
				process.Kill(entireProcessTree: true);

				return false;
			}

			return process.ExitCode == 0;
		}
		catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
		{
			return false;
		}
	}
}
