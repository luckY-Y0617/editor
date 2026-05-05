namespace Northstar.Infrastructure.Security;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public JwtOptions Jwt { get; init; } = new();
    public IdpLoginOptions IdpLogin { get; init; } = new();
    public string? SeedOwnerPassword { get; init; }

    public sealed class JwtOptions
    {
        public string Issuer { get; init; } = "Northstar";
        public string Audience { get; init; } = "Northstar";
        public string? SigningKey { get; init; }
        public int AccessTokenMinutes { get; init; } = 15;
        public int RefreshTokenDays { get; init; } = 14;
    }

    public sealed class IdpLoginOptions
    {
        public bool Enabled { get; init; }
        public string[] AllowedProviders { get; init; } = [];
    }
}
