namespace Northstar.Application.Common;

public sealed class ApplicationErrorException : Exception
{
    public ApplicationErrorException(string code, string message, object? details = null)
        : base(message)
    {
        Code = code;
        Details = details;
    }

    public string Code { get; }
    public object? Details { get; }
}

