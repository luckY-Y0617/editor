using System;
using System.Collections.Generic;
using System.Linq;

namespace NS.Framework.Authentication.Abstractions.Security;

public class PasswordValidationResult
{
	public bool Succeeded { get; }
	public IReadOnlyList<string> Errors { get; }

	public PasswordValidationResult(bool succeeded, IEnumerable<string>? errors = null)
	{
		Succeeded = succeeded;
		Errors = (errors ?? Array.Empty<string>()).ToArray();
	}

	public static PasswordValidationResult Success()
		=> new PasswordValidationResult(true);

	public static PasswordValidationResult Failed(params string[] errors)
		=> new PasswordValidationResult(false, errors);
}



