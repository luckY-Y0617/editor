using System;

namespace NS.Module.Identity.Application.Contracts.Users.Dtos;

/// <summary>
/// 用户基本信息（轻量级，供其他模块查询使用）
/// 只包含展示所需的基础字段，避免暴露敏感信息
/// </summary>
public class UserLookupDto
{
    public Guid Id { get; set; }
    
    public string UserName { get; set; } = string.Empty;
    
    public string? Email { get; set; }
    
    public string? AvatarUrl { get; set; }
    
    public Guid? TenantId { get; set; }
}

