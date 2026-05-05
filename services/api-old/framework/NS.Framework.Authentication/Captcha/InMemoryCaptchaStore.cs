using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.Authentication.Abstractions.Captcha;

namespace NS.Framework.Authentication.Captcha;

public sealed class InMemoryCaptchaStore : ICaptchaStore
{
	private readonly ConcurrentDictionary<string, (string Code, DateTimeOffset ExpiresAt)> _items = new();

	public Task StoreAsync(string key, string code, DateTimeOffset expiresAt, CancellationToken ct = default)
	{
		_items[key] = (code, expiresAt);
		return Task.CompletedTask;
	}

	public Task<string?> GetAsync(string key, CancellationToken ct = default)
	{
		if (_items.TryGetValue(key, out var value))
		{
			if (value.ExpiresAt > DateTimeOffset.UtcNow)
			{
				return Task.FromResult<string?>(value.Code);
			}
			_items.TryRemove(key, out _);
		}
		return Task.FromResult<string?>(null);
	}

	public Task<bool> RemoveAsync(string key, CancellationToken ct = default)
	{
		var removed = _items.TryRemove(key, out _);
		return Task.FromResult(removed);
	}
}


