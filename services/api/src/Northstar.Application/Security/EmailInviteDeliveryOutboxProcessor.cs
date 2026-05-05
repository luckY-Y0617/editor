using Northstar.Application.Common;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public sealed class EmailInviteDeliveryOutboxProcessor : IEmailInviteDeliveryOutboxProcessor
{
    private readonly IEmailInviteDeliveryOutboxRepository _outboxRepository;
    private readonly IEmailInviteRepository _inviteRepository;
    private readonly IEmailInviteDeliveryService _deliveryService;
    private readonly IPermissionNotificationFanoutService _notificationFanoutService;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EmailInviteDeliveryOptions _options;

    public EmailInviteDeliveryOutboxProcessor(
        IEmailInviteDeliveryOutboxRepository outboxRepository,
        IEmailInviteRepository inviteRepository,
        IEmailInviteDeliveryService deliveryService,
        IPermissionNotificationFanoutService notificationFanoutService,
        ITransactionRunner transactionRunner,
        IUnitOfWork unitOfWork,
        EmailInviteDeliveryOptions options)
    {
        _outboxRepository = outboxRepository;
        _inviteRepository = inviteRepository;
        _deliveryService = deliveryService;
        _notificationFanoutService = notificationFanoutService;
        _transactionRunner = transactionRunner;
        _unitOfWork = unitOfWork;
        _options = options;
    }

    public async Task<EmailInviteDeliveryResult> ProcessAsync(
        EmailInviteDeliveryOutboxItem item,
        ResourceEmailInvite invite,
        string acceptUrl,
        CancellationToken cancellationToken = default)
    {
        var result = await SendAsync(invite, acceptUrl, cancellationToken);
        ApplyResult(item, invite, result);
        return result;
    }

    public Task<EmailInviteDeliveryOutboxProcessResult> ProcessDueAsync(
        IReadOnlyDictionary<Guid, string> acceptUrlsByInviteId,
        DateTimeOffset? now = null,
        int batchSize = 25,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var timestamp = now ?? DateTimeOffset.UtcNow;
            var dueItems = await _outboxRepository.GetDueForUpdateAsync(
                timestamp,
                Math.Clamp(batchSize, 1, 100),
                ct);
            var attempted = 0;
            var sent = 0;
            var retrying = 0;
            var failed = 0;
            var missingAcceptUrl = 0;

            foreach (var item in dueItems)
            {
                var invite = await _inviteRepository.GetForUpdateAsync(item.InviteId, ct);
                if (invite is null)
                {
                    item.MarkTerminalFailure(item.Provider, timestamp, "invite_not_found", null);
                    failed++;
                    continue;
                }

                if (!acceptUrlsByInviteId.TryGetValue(item.InviteId, out var acceptUrl) ||
                    string.IsNullOrWhiteSpace(acceptUrl))
                {
                    item.MarkTerminalFailure(item.Provider, timestamp, "missing_accept_url", null);
                    invite.MarkDelivery(
                        EmailInviteDeliveryStatuses.Failed,
                        item.Provider,
                        timestamp,
                        "missing_accept_url");
                    missingAcceptUrl++;
                    failed++;
                    await AddDeliveryFailureNotificationAsync(invite, ct);
                    continue;
                }

                attempted++;
                var result = await ProcessAsync(item, invite, acceptUrl, ct);
                if (result.Status == EmailInviteDeliveryStatuses.Sent)
                {
                    sent++;
                    continue;
                }

                if (item.Status == EmailInviteDeliveryOutboxStatuses.RetryScheduled)
                {
                    retrying++;
                }
                else if (item.Status == EmailInviteDeliveryOutboxStatuses.Failed)
                {
                    failed++;
                }

                if (result.Status == EmailInviteDeliveryStatuses.Failed)
                {
                    await AddDeliveryFailureNotificationAsync(invite, ct);
                }
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return new EmailInviteDeliveryOutboxProcessResult(
                attempted,
                sent,
                retrying,
                failed,
                missingAcceptUrl);
        }, cancellationToken);
    }

    private async Task<EmailInviteDeliveryResult> SendAsync(
        ResourceEmailInvite invite,
        string acceptUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _deliveryService.SendAsync(
                new EmailInviteDeliveryMessage(
                    invite.Id,
                    invite.WorkspaceId,
                    invite.ResourceType,
                    invite.ResourceId,
                    invite.Email,
                    invite.RoleKey,
                    invite.ExpiresAt,
                    acceptUrl),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new EmailInviteDeliveryResult(
                EmailInviteDeliveryStatuses.Failed,
                NormalizeProvider(_options.Provider),
                DateTimeOffset.UtcNow,
                "provider_error");
        }
    }

    private void ApplyResult(
        EmailInviteDeliveryOutboxItem item,
        ResourceEmailInvite invite,
        EmailInviteDeliveryResult result)
    {
        var attemptedAt = result.AttemptedAt ?? DateTimeOffset.UtcNow;
        invite.MarkDelivery(result.Status, result.Provider, result.AttemptedAt, result.ErrorCode);
        if (result.Status == EmailInviteDeliveryStatuses.Sent)
        {
            item.MarkSent(result.Provider, attemptedAt);
            return;
        }

        if (result.Status == EmailInviteDeliveryStatuses.Disabled)
        {
            item.MarkTerminalFailure(result.Provider, attemptedAt, "delivery_disabled", null);
            return;
        }

        item.MarkFailure(
            result.Provider,
            attemptedAt,
            result.ErrorCode ?? "provider_error",
            null,
            attemptedAt.AddSeconds(Math.Max(0, _options.RetryDelaySeconds)));
    }

    private Task AddDeliveryFailureNotificationAsync(
        ResourceEmailInvite invite,
        CancellationToken cancellationToken)
    {
        return invite.InvitedBy.HasValue
            ? _notificationFanoutService.AddEmailInviteDeliveryFailedAsync(
                invite.WorkspaceId,
                invite.ResourceType,
                invite.ResourceId,
                invite.Id,
                invite.InvitedBy.Value,
                cancellationToken)
            : Task.CompletedTask;
    }

    private static string NormalizeProvider(string? provider)
    {
        return string.IsNullOrWhiteSpace(provider)
            ? "noop"
            : provider.Trim().ToLowerInvariant();
    }
}
