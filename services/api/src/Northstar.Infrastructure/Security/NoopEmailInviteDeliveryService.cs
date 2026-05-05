using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Northstar.Application.Security;
using Northstar.Domain.Security;

namespace Northstar.Infrastructure.Security;

public sealed class NoopEmailInviteDeliveryService : IEmailInviteDeliveryService
{
    private readonly EmailInviteDeliveryOptions _options;
    private readonly ILogger<NoopEmailInviteDeliveryService> _logger;

    public NoopEmailInviteDeliveryService(
        IOptions<EmailInviteDeliveryOptions> options,
        ILogger<NoopEmailInviteDeliveryService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<EmailInviteDeliveryResult> SendAsync(
        EmailInviteDeliveryMessage message,
        CancellationToken cancellationToken = default)
    {
        var provider = NormalizeProvider(_options.Provider);
        if (!_options.Enabled)
        {
            return Task.FromResult(new EmailInviteDeliveryResult(
                EmailInviteDeliveryStatuses.Disabled,
                provider,
                null));
        }

        if (provider != "noop")
        {
            _logger.LogWarning(
                "Email invite delivery provider {Provider} is not supported by the configured runtime provider.",
                provider);
            return Task.FromResult(new EmailInviteDeliveryResult(
                EmailInviteDeliveryStatuses.Failed,
                provider,
                DateTimeOffset.UtcNow,
                "unsupported_provider"));
        }

        _logger.LogInformation(
            "Email invite delivery noop provider marked invite {InviteId} for {Email} as sent.",
            message.InviteId,
            message.Email);

        return Task.FromResult(new EmailInviteDeliveryResult(
            EmailInviteDeliveryStatuses.Sent,
            provider,
            DateTimeOffset.UtcNow));
    }

    private static string NormalizeProvider(string? provider)
    {
        return string.IsNullOrWhiteSpace(provider)
            ? "noop"
            : provider.Trim().ToLowerInvariant();
    }
}
