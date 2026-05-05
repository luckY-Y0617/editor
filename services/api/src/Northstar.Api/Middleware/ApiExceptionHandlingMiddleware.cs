using System.Net;
using System.Text.Json;
using Northstar.Application.Common;
using Northstar.Contracts.Common;
using Northstar.Domain.Shared;

namespace Northstar.Api.Middleware;

public sealed class ApiExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ApiExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApplicationErrorException exception)
        {
            await WriteErrorAsync(
                context,
                StatusCodeFor(exception.Code),
                exception.Code,
                exception.Message,
                exception.Details);
        }
        catch (DomainException exception)
        {
            await WriteErrorAsync(
                context,
                StatusCodeFor(exception.Code),
                exception.Code,
                exception.Message);
        }
        catch (ArgumentException exception)
        {
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, ErrorCodes.ValidationError, exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            await WriteErrorAsync(context, HttpStatusCode.NotFound, ErrorCodes.NotFound, exception.Message);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled API exception");

            var message = _environment.IsDevelopment()
                ? exception.Message
                : "An unexpected error occurred.";

            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, ErrorCodes.InternalError, message);
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string code,
        string message,
        object? details = null)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new ApiErrorResponse(new ApiError(
            code,
            message,
            details ?? new Dictionary<string, object?>()));

        await JsonSerializer.SerializeAsync(context.Response.Body, response, JsonOptions, context.RequestAborted);
    }

    private static HttpStatusCode StatusCodeFor(string code)
    {
        return code switch
        {
            ErrorCodes.NotFound => HttpStatusCode.NotFound,
            ErrorCodes.Conflict => HttpStatusCode.Conflict,
            ErrorCodes.Unauthorized => HttpStatusCode.Unauthorized,
            ErrorCodes.Forbidden => HttpStatusCode.Forbidden,
            ErrorCodes.ValidationError => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };
    }
}
