namespace NS.Framework.Authentication.Abstractions.Security;

public interface IPasswordPolicy
{
	PasswordValidationResult Validate(string password);
}


