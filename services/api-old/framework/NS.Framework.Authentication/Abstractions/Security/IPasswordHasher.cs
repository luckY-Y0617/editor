namespace NS.Framework.Authentication.Abstractions.Security;

public interface IPasswordHasher
{
	string Hash(string password);
	PasswordVerificationResult Verify(string hashedPassword, string providedPassword);
}

public enum PasswordVerificationResult
{
	Failed = 0,
	Success = 1,
	SuccessRehashNeeded = 2
}


