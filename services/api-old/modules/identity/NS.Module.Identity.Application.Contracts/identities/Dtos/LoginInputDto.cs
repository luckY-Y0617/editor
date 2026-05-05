namespace NS.Module.Identity.Application.Contracts.identities.Dtos;

public sealed class LoginInputDto
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public string? SessionId { get; set; }
    public string? ClientId { get; set; }
    public string? DeviceId { get; set; }
    public string? Fingerprint { get; set; }

    public string? LoginLocation { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? DeviceType { get; set; }
    public string? DeviceModel { get; set; }
    public string? AppVersion { get; set; }
    public string? AppChannel { get; set; }
    public string? NetworkType { get; set; }
    public string? LoginSource { get; set; }
}