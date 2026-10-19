using System.Globalization;
using EtcdTerminal.App.Components;
using EtcdTerminal.Cluster;
using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Cluster;

/// <summary>
/// Builds the rows of the cluster page: one row per etcd member with its
/// health, identity, version, raft progress, database usage and reported
/// errors. Literal text and semantic roles only; column widths and the table
/// geometry belong to Infrastructure.
/// </summary>
public sealed class ClusterStatusLayout(ILocalizationCatalog _localizations)
{
	public string Loading => _localizations.Current.LoadingClusterStatus;

	public string Empty => _localizations.Current.NoNodesFound;

	public string Total => _localizations.Current.TotalNodes;

	public IReadOnlyList<StyledText> Headers() =>
	[
		new StyledText(_localizations.Current.NodeName, TextRole.Accent),
		new StyledText(_localizations.Current.Healthy, TextRole.Accent),
		new StyledText(_localizations.Current.MemberId, TextRole.Accent),
		new StyledText(_localizations.Current.Version, TextRole.Accent),
		new StyledText(_localizations.Current.Leader, TextRole.Accent),
		new StyledText(_localizations.Current.DbSize, TextRole.Accent),
		new StyledText(_localizations.Current.DbSizeInUse, TextRole.Accent),
		new StyledText(_localizations.Current.PercentageNotInUse, TextRole.Accent),
		new StyledText(_localizations.Current.RaftTerm, TextRole.Accent),
		new StyledText(_localizations.Current.RaftIndex, TextRole.Accent),
		new StyledText(_localizations.Current.RaftAppliedIndex, TextRole.Accent),
		new StyledText(_localizations.Current.Errors, TextRole.Accent)
	];

	public IReadOnlyList<IReadOnlyList<StyledText>> Rows(IReadOnlyList<EtcdClusterMember> members) =>
		[.. members.Select(Row)];

	private IReadOnlyList<StyledText> Row(EtcdClusterMember member) =>
	[
		Cell(Value(member.Name)),
		Cell(YesNo(member.IsHealthy)),
		Cell(member.Id.ToString("x", CultureInfo.InvariantCulture)),
		Cell(Value(member.Version)),
		Cell(YesNo(member.IsLeader)),
		Cell(Metric(member, FormatBytes(member.DbSize))),
		Cell(Metric(member, FormatBytes(member.DbSizeInUse))),
		Cell(PercentageNotInUse(member)),
		Cell(Metric(member, member.RaftTerm.ToString(CultureInfo.InvariantCulture))),
		Cell(Metric(member, member.RaftIndex.ToString(CultureInfo.InvariantCulture))),
		Cell(Metric(member, member.RaftAppliedIndex.ToString(CultureInfo.InvariantCulture))),
		Cell(Errors(member))
	];

	private string Value(string value) =>
		value.Length == 0 ? _localizations.Current.None : DisplayText.Sanitize(value);

	private string YesNo(bool value) => value ? _localizations.Current.Yes : _localizations.Current.No;

	private string Metric(EtcdClusterMember member, string value) =>
		member.HasStatus ? value : _localizations.Current.None;

	private string PercentageNotInUse(EtcdClusterMember member)
	{
		if (!member.HasStatus || member.DbSize <= 0)
			return _localizations.Current.None;

		var percent = Math.Clamp((member.DbSize - member.DbSizeInUse) * 100d / member.DbSize, 0d, 100d);
		var rounded = Math.Round(percent, 1, MidpointRounding.AwayFromZero);

		return rounded.ToString("0.0", CultureInfo.InvariantCulture) + "%";
	}

	private string Errors(EtcdClusterMember member) =>
		member.Errors.Count == 0
			? _localizations.Current.None
			: string.Join("; ", member.Errors.Select(DisplayText.Sanitize));

	private static string FormatBytes(long bytes)
	{
		string[] units = ["B", "KB", "MB", "GB", "TB"];
		var value = (double)bytes;
		var unit = 0;

		while (value >= 1000 && unit < units.Length - 1)
		{
			value /= 1000;
			unit++;
		}

		return $"{Math.Round(value, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture)} {units[unit]}";
	}

	private static StyledText Cell(string text) => new(text, TextRole.Primary);
}
