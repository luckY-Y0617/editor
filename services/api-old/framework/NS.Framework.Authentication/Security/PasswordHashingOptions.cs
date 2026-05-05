namespace NS.Framework.Authentication.Security;

public sealed class PasswordHashingOptions
{
	public string AlgorithmVersion { get; set; } = "PBKDF2-SHA256";
	public int IterationCount { get; set; } = 100_000;
	public int SaltSize { get; set; } = 16;
	public int KeySize { get; set; } = 32;
}


