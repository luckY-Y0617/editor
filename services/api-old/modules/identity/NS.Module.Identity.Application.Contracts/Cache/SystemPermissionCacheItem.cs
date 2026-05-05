using System;
using System.Collections.Generic;

namespace NS.Module.Identity.Application.Contracts.Cache;

[Serializable]
public sealed class SystemPermissionCacheItem
{
    public Guid UserId { get; init; }
    public Guid? TenantId { get; init; }

    public HashSet<string> PermissionCodes { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public DateTime CachedAtUtc { get; init; } = DateTime.UtcNow;
}