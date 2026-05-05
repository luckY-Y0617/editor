using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Northstar.Application.Security;
using Northstar.Domain.Security;

namespace Northstar.Infrastructure.Security;

public sealed class SmtpEmailInviteDeliveryService : IEmailInviteDeliveryService
{
    private const string Provider = "smtp";

    private readonly EmailInviteDeliveryOptions _options;
    private readonly ILogger<SmtpEmailInviteDeliveryService> _logger;

    public SmtpEmailInviteDeliveryService(
        IOptions<EmailInviteDeliveryOptions> options,
        ILogger<SmtpEmailInviteDeliveryService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EmailInviteDeliveryResult> SendAsync(
        EmailInviteDeliveryMessage message,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return new EmailInviteDeliveryResult(
                EmailInviteDeliveryStatuses.Disabled,
                Provider,
                null);
        }

        var attemptedAt = DateTimeOffset.UtcNow;
        if (!TryValidateConfiguration(out var error))
        {
            _logger.LogWarning(
                "SMTP invite delivery configuration is invalid for invite {InviteId}: {ErrorCode}.",
                message.InviteId,
                error);
            return new EmailInviteDeliveryResult(
                EmailInviteDeliveryStatuses.Failed,
                Provider,
                attemptedAt,
                error);
        }

        try
        {
            using var mail = CreateMessage(message);
            using var client = CreateClient();
            await client.SendMailAsync(mail, cancellationToken);
            return new EmailInviteDeliveryResult(
                EmailInviteDeliveryStatuses.Sent,
                Provider,
                attemptedAt);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "SMTP invite delivery failed for invite {InviteId}.",
                message.InviteId);
            return new EmailInviteDeliveryResult(
                EmailInviteDeliveryStatuses.Failed,
                Provider,
                attemptedAt,
                "provider_error");
        }
    }

    private bool TryValidateConfiguration(out string? errorCode)
    {
        if (string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            errorCode = "configuration_error";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_options.Smtp.Host))
        {
            errorCode = "configuration_error";
            return false;
        }

        if (_options.Smtp.Port <= 0 || _options.Smtp.TimeoutSeconds <= 0)
        {
            errorCode = "configuration_error";
            return false;
        }

        errorCode = null;
        return true;
    }

    private MailMessage CreateMessage(EmailInviteDeliveryMessage message)
    {
        var from = string.IsNullOrWhiteSpace(_options.FromName)
            ? new MailAddress(_options.FromEmail!)
            : new MailAddress(_options.FromEmail!, _options.FromName!.Trim());

        var mail = new MailMessage
        {
            From = from,
            Subject = "Northstar access invitation",
            Body = CreateBody(message),
            IsBodyHtml = false
        };
        mail.To.Add(new MailAddress(message.Email));
        return mail;
    }

    private SmtpClient CreateClient()
    {
        var client = new SmtpClient(_options.Smtp.Host!.Trim(), _options.Smtp.Port)
        {
            EnableSsl = _options.Smtp.UseSsl,
            Timeout = checked(_options.Smtp.TimeoutSeconds * 1000)
        };

        if (!string.IsNullOrWhiteSpace(_options.Smtp.Username))
        {
            client.Credentials = new NetworkCredential(
                _options.Smtp.Username.Trim(),
                _options.Smtp.Password ?? string.Empty);
        }

        return client;
    }

    private static string CreateBody(EmailInviteDeliveryMessage message)
    {
        return string.Join(
            Environment.NewLine,
            "You have been invited to access a Northstar resource.",
            string.Empty,
            $"Resource type: {message.ResourceType}",
            $"Role: {message.RoleKey}",
            $"Expires at: {message.ExpiresAt:O}",
            string.Empty,
            "Accept the invitation:",
            message.AcceptUrl);
    }
}
