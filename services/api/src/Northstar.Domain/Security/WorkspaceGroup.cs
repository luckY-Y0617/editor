using Northstar.Domain.Shared;

namespace Northstar.Domain.Security;

public sealed class WorkspaceGroup
{
    private WorkspaceGroup()
    {
        Name = string.Empty;
        Type = GroupTypes.Static;
    }

    public WorkspaceGroup(
        Guid workspaceId,
        string name,
        string? description = null,
        string type = GroupTypes.Static,
        Guid? createdBy = null,
        string? externalProvider = null,
        string? externalGroupId = null,
        DateTimeOffset? externalSyncedAt = null,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        Name = ValidName(name);
        Description = NormalizeOptional(description);
        Type = ValidType(type);
        ExternalProvider = NormalizeOptional(externalProvider);
        ExternalGroupId = NormalizeOptional(externalGroupId);
        if ((ExternalProvider is null) != (ExternalGroupId is null))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "external provider and group id must be provided together.");
        }

        ExternalSyncedAt = IsExternal ? externalSyncedAt ?? DateTimeOffset.UtcNow : null;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string Type { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public string? ExternalProvider { get; private set; }
    public string? ExternalGroupId { get; private set; }
    public DateTimeOffset? ExternalSyncedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsArchived => ArchivedAt.HasValue;
    public bool IsExternal => ExternalProvider is not null && ExternalGroupId is not null;

    public void Update(string name, string? description)
    {
        if (IsExternal)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "external groups are read-only.");
        }

        if (IsArchived)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "archived groups cannot be updated.");
        }

        Name = ValidName(name);
        Description = NormalizeOptional(description);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Archive()
    {
        if (IsExternal)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "external groups are read-only.");
        }

        if (ArchivedAt.HasValue)
        {
            return;
        }

        ArchivedAt = DateTimeOffset.UtcNow;
        UpdatedAt = ArchivedAt.Value;
    }

    public bool SyncExternal(
        string name,
        string? description,
        string externalProvider,
        string externalGroupId,
        DateTimeOffset syncedAt)
    {
        if (IsArchived)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "archived groups cannot be synced.");
        }

        var provider = ValidExternalValue(externalProvider, nameof(externalProvider));
        var groupId = ValidExternalValue(externalGroupId, nameof(externalGroupId));
        if (IsExternal && (ExternalProvider != provider || ExternalGroupId != groupId))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "external group identity cannot be changed.");
        }

        var normalizedName = ValidName(name);
        var normalizedDescription = NormalizeOptional(description);
        var changed =
            Name != normalizedName ||
            Description != normalizedDescription ||
            Type != GroupTypes.Dynamic ||
            ExternalProvider != provider ||
            ExternalGroupId != groupId;

        Name = normalizedName;
        Description = normalizedDescription;
        Type = GroupTypes.Dynamic;
        ExternalProvider = provider;
        ExternalGroupId = groupId;
        ExternalSyncedAt = syncedAt;
        UpdatedAt = syncedAt;

        return changed;
    }

    private static string ValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "group name is required.");
        }

        return name.Trim();
    }

    private static string ValidType(string type)
    {
        var normalized = type.Trim().ToLowerInvariant();
        return GroupTypes.IsSupported(normalized)
            ? normalized
            : throw new DomainException(DomainErrorCodes.ValidationError, "group type is invalid.");
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string ValidExternalValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, $"{parameterName} is required.");
        }

        return value.Trim();
    }
}
