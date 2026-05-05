using System;

namespace NS.Module.Identity.Application.Contracts.identities.Dtos;

public sealed class AppLoginOutputDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAtUtc { get; set; }

    public AuthUserDto User { get; set; } = default!;
}
