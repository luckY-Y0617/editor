using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace NS.Framework.Authentication.Token;

public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public ClaimsPrincipal? Principal { get; set; }  // 验证成功时的 Claims
    public string? ErrorMessage { get; set; }       // 验证失败时的错误信息
    public SecurityTokenException? Exception { get; set; }  // 异常信息
}

