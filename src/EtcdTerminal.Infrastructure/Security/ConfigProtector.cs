using EtcdTerminal.Security;
using System.Security.Cryptography;
using System.Text;

namespace EtcdTerminal.Infrastructure.Security;

public sealed class ConfigProtector(IAppEnvironment environment) : IConfigProtector
{
	private const string Prefix = "$AES$";
	private const int KeyLength = 32;
	private const int NonceLength = 12;
	private const int TagLength = 16;

	private readonly byte[] _key = LoadOrCreateKey(environment.KeyFilePath);

	public string Encrypt(string plainText)
	{
		var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
		var nonce = RandomNumberGenerator.GetBytes(NonceLength);
		var cipherText = new byte[plainTextBytes.Length];
		var tag = new byte[TagLength];

		using var aes = new AesGcm(_key, TagLength);

		aes.Encrypt(nonce, plainTextBytes, cipherText, tag);

		var combined = new byte[NonceLength + TagLength + cipherText.Length];

		nonce.CopyTo(combined, 0);
		tag.CopyTo(combined, NonceLength);
		cipherText.CopyTo(combined, NonceLength + TagLength);

		return Prefix + Convert.ToBase64String(combined);
	}

	public string? Decrypt(string? cipherText)
	{
		if (string.IsNullOrEmpty(cipherText))
			return cipherText;

		if (!cipherText.StartsWith(Prefix, StringComparison.Ordinal))
			return cipherText;

		var raw = Convert.FromBase64String(cipherText[Prefix.Length..]);
		var nonce = raw.AsSpan(0, NonceLength);
		var tag = raw.AsSpan(NonceLength, TagLength);
		var encrypted = raw[(NonceLength + TagLength)..];
		var plainTextBytes = new byte[encrypted.Length];

		using var aes = new AesGcm(_key, TagLength);

		aes.Decrypt(nonce, encrypted, tag, plainTextBytes);

		return Encoding.UTF8.GetString(plainTextBytes);
	}

	private static byte[] LoadOrCreateKey(string keyPath)
	{
		if (File.Exists(keyPath))
			return File.ReadAllBytes(keyPath);

		var dir = Path.GetDirectoryName(keyPath);
		Directory.CreateDirectory(dir!);

		var key = RandomNumberGenerator.GetBytes(KeyLength);
		File.WriteAllBytes(keyPath, key);

		if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
			File.SetUnixFileMode(keyPath,
				UnixFileMode.UserRead | UnixFileMode.UserWrite);

		return key;
	}
}
