namespace Northstar.Contracts.Common;

public sealed record ApiErrorResponse(ApiError Error);

public sealed record ApiError(string Code, string Message, object? Details = null);

