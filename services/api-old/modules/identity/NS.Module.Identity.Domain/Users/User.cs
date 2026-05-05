using System;
using System.Collections.Generic;
using SqlSugar;
using Volo.Abp.MultiTenancy;
using Check = Volo.Abp.Check;
using NS.Framework.Core.Abstractions.Domain;
using NS.Module.Identity.Domain.Roles;
using NS.Module.Identity.Domain.Shared.Consts;
using Volo.Abp.Domain.Entities.Auditing;

namespace NS.Module.Identity.Domain.Users;

[SugarTable("id_users")]
[SugarIndex("IX_Users_UserName", nameof(NormalizedUserName), OrderByType.Asc)]
[SugarIndex("IX_Users_Email", nameof(NormalizedEmail), OrderByType.Asc)]
[SugarIndex("IX_Users_Phone", nameof(NormalizedPhoneNumber), OrderByType.Asc)]
[SugarIndex("IX_Users_Tenant", nameof(TenantId), OrderByType.Asc)]
public class User : FullAuditedAggregateRoot<Guid>,  IMultiTenant, IState
{
    public string UserName { get; private set; } = string.Empty;
    public string NormalizedUserName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    
    [SugarColumn(IsNullable = true)]
    public string? Email { get; private set; }
    
    [SugarColumn(IsNullable = true)]
    public string? NormalizedEmail { get; private set; }
    public bool EmailConfirmed { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? PhoneNumber { get; private set; }
    
    [SugarColumn(IsNullable = true)]
    public string? NormalizedPhoneNumber { get; private set; }
    public bool PhoneNumberConfirmed { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public bool LockoutEnabled { get; private set; }
    
    [SugarColumn(IsNullable = true)]
    public DateTime? LockoutEnd { get; private set; }
    public int AccessFailedCount { get; private set; }
    
    public bool IsEnabled { get; private set; } = true;
    
    [SugarColumn(IsNullable = true)]
    public Guid? TenantId { get; set; }

    [Navigate(typeof(UserRole), nameof(UserRole.UserId), nameof(UserRole.RoleId))]
    public List<Role> Roles { get; set; } = null!;

    [Navigate(NavigateType.OneToOne, nameof(Id), nameof(UserProfile.UserId))]
    public UserProfile Profile { get; set; } = null!;

    public User() { }

    public User(string userName, string passwordHash)
    {
        SetUserName(userName);
        SetPassword(passwordHash);
    }

    public bool IsLockedOut()
    {
        if (LockoutEnabled &&
            LockoutEnd.HasValue &&
            LockoutEnd > DateTime.UtcNow)
        {
            return true;
        }
        
        return false;
    }

    public void SetUserName(string userName)
    {
        Check.NotNullOrWhiteSpace(userName, nameof(userName), UserConsts.UserNameMaxLength);
        UserName = userName;
        NormalizedUserName = userName.ToUpperInvariant();
    }

    public void SetPassword(string passwordHash)
    {
        Check.NotNullOrWhiteSpace(passwordHash, nameof(passwordHash), UserConsts.PasswordHashMaxLength);
        PasswordHash = passwordHash;
    }

    public void SetEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            Email = null;
            NormalizedEmail = null;
            EmailConfirmed = false;
            return;
        }

        Check.Length(email, nameof(email), UserConsts.EmailMaxLength);
        Email = email;
        NormalizedEmail = email.ToUpperInvariant();
    }

    public void ConfirmEmail()
    {
        if (!string.IsNullOrWhiteSpace(Email))
        {
            EmailConfirmed = true;
        }
    }

    public void SetPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            PhoneNumber = null;
            NormalizedPhoneNumber = null;
            PhoneNumberConfirmed = false;
            return;
        }

        Check.Length(phoneNumber, nameof(phoneNumber), UserConsts.PhoneNumberMaxLength);
        PhoneNumber = phoneNumber;
        NormalizedPhoneNumber = NormalizePhoneNumber(phoneNumber);
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        // 移除所有非数字字符（保留 + 号用于国际号码）
        var normalized = phoneNumber.Trim();
        
        // 处理中国手机号：移除 +86、空格、横线等
        if (normalized.StartsWith("+86", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(3).TrimStart();
        }
        
        // 移除所有非数字字符
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^\d]", "");
        
        return normalized;
    }

    public void ConfirmPhoneNumber()
    {
        if (!string.IsNullOrWhiteSpace(PhoneNumber))
        {
            PhoneNumberConfirmed = true;
        }
    }

    public void EnableTwoFactor()
    {
        TwoFactorEnabled = true;
    }

    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
    }

    public void EnableLockout(DateTime? lockoutEnd)
    {
        LockoutEnabled = true;
        LockoutEnd = lockoutEnd;
    }

    public void DisableLockout()
    {
        LockoutEnabled = false;
        LockoutEnd = null;
        ResetAccessFailedCount();
    }

    public void IncreaseAccessFailedCount()
    {
        AccessFailedCount++;
    }

    public void ResetAccessFailedCount()
    {
        AccessFailedCount = 0;
    }

    public void Activate()
    {
        IsEnabled = true;
    }

    public void Deactivate()
    {
        IsEnabled = false;
    }

    public void RefreshConcurrencyStamp()
    {
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }
}

