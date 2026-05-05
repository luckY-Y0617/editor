using System;
using System.Threading;
using System.Threading.Tasks;

namespace NS.Framework.Authentication.Session;

public interface ISessionStore
{
    Task<string> CreateAsync(SessionPayload payload, CancellationToken ct = default);
    Task<SessionPayload?> GetAsync(string sessionId, CancellationToken ct = default);
    Task RefreshAsync(string sessionId, TimeSpan ttl, CancellationToken ct = default);
    Task RevokeAsync(string sessionId, CancellationToken ct = default);
}