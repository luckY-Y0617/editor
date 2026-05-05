namespace NS.Framework.Authentication.Abstractions.Security;

public sealed class CredentialsValidationResult
{
	private CredentialsValidationResult(bool succeeded, bool isLockedOut, string? failureReason)
	{
		Succeeded = succeeded;
		IsLockedOut = isLockedOut;
		FailureReason = failureReason;
	}

	public bool Succeeded { get; }
	public bool IsLockedOut { get; }
	public string? FailureReason { get; }

	public static CredentialsValidationResult Success() => new(true, false, null);

	public static CredentialsValidationResult LockedOut(string? reason = null) => new(false, true, reason ?? "Too many failed attempts.");

	public static CredentialsValidationResult Failed(string? reason = null) =>
		new(false, false, reason ?? "Credential validation failed.");
}


