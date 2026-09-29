using EtcdTerminal.Presentation;

namespace EtcdTerminal.Tests.Fakes;

/// <summary>
/// Captures footer models instead of drawing them. Fitting and placement belong
/// to Infrastructure, so a model test must not assert on rendered width.
/// </summary>
public sealed class FakeStatusBarRenderer : IStatusBarRenderer
{
	public List<StatusBarModel> Models { get; } = [];

	public List<int> RoomRequests { get; } = [];

	public int ClearRequests { get; private set; }

	public void Write(StatusBarModel model) => Models.Add(model);

	public void EnsureRoomAbove(int rows) => RoomRequests.Add(rows);

	public void ClearBelow() => ClearRequests++;

	public StatusBarModel Last => Models[^1];
}
