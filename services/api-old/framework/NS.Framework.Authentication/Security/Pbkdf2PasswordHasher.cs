using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using NS.Framework.Authentication.Abstractions.Security;

namespace NS.Framework.Authentication.Security;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
	private readonly PasswordHashingOptions _options;

	public Pbkdf2PasswordHasher(IOptions<PasswordHashingOptions> options)
	{
		_options = options.Value;
	}

	public string Hash(string password)
	{
		if (string.IsNullOrEmpty(password))
		{
			throw new ArgumentException("Password cannot be null or empty.", nameof(password));
		}

		var salt = RandomNumberGenerator.GetBytes(_options.SaltSize);
		var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, _options.IterationCount, HashAlgorithmName.SHA256, _options.KeySize);

		return string.Join('.',
			_options.AlgorithmVersion,
			_options.IterationCount.ToString(),
			Convert.ToBase64String(salt),
			Convert.ToBase64String(hash));
	}

	public PasswordVerificationResult Verify(string hashedPassword, string providedPassword)
	{
		if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(providedPassword))
		{
			return PasswordVerificationResult.Failed;
		}

		var parts = hashedPassword.Split('.', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length != 4)
		{
			return PasswordVerificationResult.Failed;
		}

		var version = parts[0];
		if (!int.TryParse(parts[1], out var iterations))
		{
			return PasswordVerificationResult.Failed;
		}

		var salt = Convert.FromBase64String(parts[2]);
		var storedHash = Convert.FromBase64String(parts[3]);

		var computed = Rfc2898DeriveBytes.Pbkdf2(providedPassword, salt, iterations, HashAlgorithmName.SHA256, storedHash.Length);

		var matches = CryptographicOperations.FixedTimeEquals(storedHash, computed);
		if (!matches)
		{
			return PasswordVerificationResult.Failed;
		}

		var requiresUpgrade = !string.Equals(version, _options.AlgorithmVersion, StringComparison.Ordinal) ||
			iterations < _options.IterationCount ||
			salt.Length < _options.SaltSize ||
			storedHash.Length < _options.KeySize;

		return requiresUpgrade ? PasswordVerificationResult.SuccessRehashNeeded : PasswordVerificationResult.Success;
	}
}


