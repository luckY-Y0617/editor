namespace NS.Framework.Authentication.Abstractions.Captcha;

public interface ICaptchaStore
{
	Task StoreAsync(string key, string code, DateTimeOffset expiresAt, CancellationToken ct = default);
	Task<string?> GetAsync(string key, CancellationToken ct = default);
	Task<bool> RemoveAsync(string key, CancellationToken ct = default);
}


