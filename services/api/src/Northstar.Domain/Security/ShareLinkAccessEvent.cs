using System.Text.Json;
using Northstar.Domain.Shared;

namespace Northstar.Domain.Security;

public sealed class ShareLinkAccessEvent
{
    private ShareLinkAccessEvent()
    {
        ResourceType = ResourceTypes.Document;
        Audience = ShareLinkAudiences.Workspace;
        EventType = ShareLinkAccessEventTypes.Access;
        Result = ShareLinkAccessResults.Success;
        Metadata = "{}";
    }

    public ShareLinkAccessEvent(
        Guid workspaceId,
        Guid shareLinkId,
        string resourceType,
        Guid resourceId,
        Guid? actorUserId,
        string audience,
        string eventType,
        string result,
        string? failureCategory,
        string? remoteIp,
        string? userAgent,
        string? metadata = null,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        ShareLinkId = shareLinkId;
        ResourceType = ValidResourceType(resourceType);
        ResourceId = resourceId;
        ActorUserId = actorUserId;
        Audience = ValidAudience(audience);
        EventType = ValidEventType(eventType);
        Result = ValidResult(result);
        FailureCategory = NormalizeOptional(failureCategory);
        RemoteIp = NormalizeOptional(remoteIp);
        UserAgent = NormalizeOptional(userAgent);
        OccurredAt = DateTimeOffset.UtcNow;
        Metadata = NormalizeMetadata(metadata);
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid ShareLinkId { get; private set; }
    public string ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string Audience { get; private set; }
    public string EventType { get; private set; }
    public string Result { get; private set; }
    public string? FailureCategory { get; private set; }
    public string? RemoteIp { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string Metadata { get; private set; }

    private static string ValidResourceType(string resourceType)
    {
        return ResourceTypes.IsShareableResource(resourceType)
            ? resourceType
            : throw new DomainException(DomainErrorCodes.ValidationError, "resource type is invalid.");
    }

    private static string ValidAudience(string audience)
    {
        return ShareLinkAudiences.IsSupported(audience)
            ? audience
            : throw new DomainException(DomainErrorCodes.ValidationError, "share link audience is invalid.");
    }

    private static string ValidEventType(string eventType)
    {
        return ShareLinkAccessEventTypes.IsSupported(eventType)
            ? eventType
            : throw new DomainException(DomainErrorCodes.ValidationError, "share link access event type is invalid.");
    }

    private static string ValidResult(string result)
    {
        return ShareLinkAccessResults.IsSupported(result)
            ? result
            : throw new DomainException(DomainErrorCodes.ValidationError, "share link access result is invalid.");
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeMetadata(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return "{}";
        }

        using var _ = JsonDocument.Parse(metadata);
        return metadata.Trim();
    }
}

public static class ShareLinkAccessEventTypes
{
    public const string Resolve = "resolve";
    public const string Access = "access";
    public const string Download = "download";

    public static bool IsSupported(string? eventType)
    {
        return eventType is Resolve or Access or Download;
    }
}

public static class ShareLinkAccessResults
{
    public const string Success = "success";
    public const string Fail = "fail";

    public static bool IsSupported(string? result)
    {
        return result is Success or Fail;
    }
}
