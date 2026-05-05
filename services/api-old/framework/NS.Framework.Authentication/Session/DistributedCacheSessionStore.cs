using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace NS.Framework.Authentication.Session;

public sealed class DistributedCacheSessionStore : ISessionStore
{
    private readonly IDistributedCache _cache;
    private readonly AuthOptions _authOptions;
    private static readonly JsonSerializerOptions JsonOpt = new(JsonSerializerDefaults.Web);

    public DistributedCacheSessionStore(IDistributedCache cache, IOptions<AuthOptions> authOptions)
    {
        _cache = cache;
        _authOptions = authOptions.Value;
    }

    public async Task<string> CreateAsync(SessionPayload payload, CancellationToken ct = default)
    {
        var sid = Guid.NewGuid().ToString("N");
        var key = Key(sid);

        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOpt);
        await _cache.SetAsync(key, bytes, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _authOptions.Session.Ttl
        }, ct);

        return sid;
    }

    public async Task<SessionPayload?> GetAsync(string sessionId, CancellationToken ct = default)
    {
        var bytes = await _cache.GetAsync(Key(sessionId), ct);
        if (bytes is null || bytes.Length == 0) return null;

        return JsonSerializer.Deserialize<SessionPayload>(bytes, JsonOpt);
    }

    public Task RefreshAsync(string sessionId, TimeSpan ttl, CancellationToken ct = default)
    {
        // IDistributedCache 没有“只改过期”的通用 API；
        // 最稳做法是 Get -> Set 重写同值（你也可以换 Redis 客户端实现更优）
        return RefreshByRewriteAsync(sessionId, ttl, ct);
    }

    private async Task RefreshByRewriteAsync(string sessionId, TimeSpan ttl, CancellationToken ct)
    {
        var key = Key(sessionId);
        var bytes = await _cache.GetAsync(key, ct);
        if (bytes is null) return;

        await _cache.SetAsync(key, bytes, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        }, ct);
    }

    public Task RevokeAsync(string sessionId, CancellationToken ct = default)
        => _cache.RemoveAsync(Key(sessionId), ct);

    private string Key(string sid) => _authOptions.Session.CacheKeyPrefix + sid;
}
