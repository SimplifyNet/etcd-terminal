using EtcdTerminal.Clipboard;
using EtcdTerminal.Infrastructure.Clipboard;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ProcessClipboardTests
{
	[Test]
	public void Copy_WithoutAnyPlatformTool_FallsBackAndReportsItsResult()
	{
		using var _ = new NoDisplay();

		var fallback = new RecordingClipboard(result: true);
		var clipboard = new ProcessClipboard(fallback);

		var copied = clipboard.Copy("hello");

		Assert.Multiple(() =>
		{
			Assert.That(copied, Is.True);
			Assert.That(fallback.Copied, Is.EqualTo(new[] { "hello" }));
		});
	}

	[Test]
	public void Copy_FallbackRefuses_ReportsFailure()
	{
		using var _ = new NoDisplay();

		var clipboard = new ProcessClipboard(new RecordingClipboard(result: false));

		Assert.That(clipboard.Copy("hello"), Is.False);
	}

	/// <summary>
	/// Hides the display variables for the duration of a test so no Linux
	/// clipboard tool is tried and the developer's clipboard is left alone.
	/// On Windows and macOS the platform tool is unconditional, so the
	/// fallback assertions there are skipped.
	/// </summary>
	private sealed class NoDisplay : IDisposable
	{
		private readonly string? _wayland = System.Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
		private readonly string? _x11 = System.Environment.GetEnvironmentVariable("DISPLAY");

		public NoDisplay()
		{
			if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
				Assert.Ignore("The platform clipboard tool is always tried on this OS.");

			System.Environment.SetEnvironmentVariable("WAYLAND_DISPLAY", null);
			System.Environment.SetEnvironmentVariable("DISPLAY", null);
		}

		public void Dispose()
		{
			System.Environment.SetEnvironmentVariable("WAYLAND_DISPLAY", _wayland);
			System.Environment.SetEnvironmentVariable("DISPLAY", _x11);
		}
	}

	private sealed class RecordingClipboard(bool result) : IClipboard
	{
		public List<string> Copied { get; } = [];

		public bool Copy(string text)
		{
			Copied.Add(text);

			return result;
		}
	}
}
