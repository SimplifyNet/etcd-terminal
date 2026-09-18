namespace EtcdTerminal.Configuration;

public interface IDecryptFailureSource
{
	IReadOnlyList<string> TakeDecryptFailures();
}
