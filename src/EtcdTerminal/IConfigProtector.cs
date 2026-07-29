namespace EtcdTerminal;

public interface IConfigProtector
{
	string Encrypt(string plainText);
	string? Decrypt(string? cipherText);
}
