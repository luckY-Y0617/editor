namespace Northstar.Domain.Security;

public sealed class ShareLinkAccessStats
{
    private ShareLinkAccessStats()
    {
    }

    public ShareLinkAccessStats(Guid workspaceId, Guid shareLinkId)
    {
        WorkspaceId = workspaceId;
        ShareLinkId = shareLinkId;
    }

    public Guid WorkspaceId { get; private set; }
    public Guid ShareLinkId { get; private set; }
    public DateTimeOffset? LastAccessedAt { get; private set; }
    public long AccessCount { get; private set; }
    public long UniqueVisitorCount { get; private set; }
    public string? LastAccessIp { get; private set; }

    public void RecordSuccessfulAccess(DateTimeOffset occurredAt, string? remoteIp, bool newAuthenticatedVisitor)
    {
        LastAccessedAt = occurredAt;
        AccessCount++;
        LastAccessIp = string.IsNullOrWhiteSpace(remoteIp) ? null : remoteIp.Trim();
        if (newAuthenticatedVisitor)
        {
            UniqueVisitorCount++;
        }
    }
}
