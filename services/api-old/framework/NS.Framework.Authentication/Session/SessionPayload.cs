using System;

namespace NS.Framework.Authentication.Session;

public sealed class SessionPayload
{
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string UserName { get; set; } = "";
    public string[] Roles { get; set; } = Array.Empty<string>();

    public DateTimeOffset IssuedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }

    // 可选：权限版本/安全版本（权限变更可强制失效）
    public long SecurityStamp { get; set; } = 0;
}