using EtcdTerminal.Configuration;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ConnectionNamesTests
{
	private static readonly IReadOnlyList<EtcdConnectionConfig> Instances =
	[
		Named("prod"),
		Named("staging")
	];

	[Test]
	public void IsTaken_FreeName_IsNotTaken()
	{
		var taken = ConnectionNames.IsTaken(Instances, "dev", null);

		Assert.That(taken, Is.False);
	}

	[Test]
	public void IsTaken_NameOfAnotherInstance_IsTaken()
	{
		var taken = ConnectionNames.IsTaken(Instances, "staging", null);

		Assert.That(taken, Is.True);
	}

	[Test]
	public void IsTaken_TheInstanceBeingEdited_KeepsItsOwnName()
	{
		var taken = ConnectionNames.IsTaken(Instances, "prod", "prod");

		Assert.That(taken, Is.False);
	}

	private static EtcdConnectionConfig Named(string name) => new() { Name = name };
}
