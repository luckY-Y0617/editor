using System;
using NS.Module.Identity.Domain.Shared.Consts;
using NS.Module.Identity.Domain.Shared.Enums;
using SqlSugar;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Check = Volo.Abp.Check;

namespace NS.Module.Identity.Domain.Users;

[SugarTable("id_user_profiles")]
[SugarIndex("IX_UserProfiles_UserId", nameof(UserId), OrderByType.Asc, true)]
public class UserProfile : AuditedEntity<Guid>, IHasConcurrencyStamp
{
    public Guid UserId { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? NickName { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? Avatar { get; private set; }
    public GenderEnum Gender { get; private set; } = GenderEnum.Unknown;
    
    [SugarColumn(IsNullable = true)]
    public DateTime? Birthday { get; private set; }
    
    [SugarColumn(IsNullable = true)]
    public int? Age { get; private set; }
    
    [SugarColumn(IsNullable = true)]
    public string? Address { get; private set; }
    
    [SugarColumn(IsNullable = true)]
    public string? Introduction { get; private set; }
    
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");
    
    public UserProfile()
    {
    }

    public UserProfile(Guid userId)
    {
        UserId = userId;
    }

    public void SetNickName(string? nickName)
    {
        if (string.IsNullOrWhiteSpace(nickName))
        {
            NickName = null;
            return;
        }

        Check.Length(nickName, nameof(nickName), UserConsts.NickNameMaxLength);
        NickName = nickName;
    }

    public void SetAvatar(string? avatar)
    {
        if (string.IsNullOrWhiteSpace(avatar))
        {
            Avatar = null;
            return;
        }

        Check.Length(avatar, nameof(avatar), UserConsts.AvatarMaxLength);
        Avatar = avatar;
    }

    public void SetGender(GenderEnum gender)
    {
        Gender = gender;
    }

    public void SetBirthday(DateTime? birthday)
    {
        Birthday = birthday;
    }

    public void SetAge(int? age)
    {
        if (age.HasValue)
        {
            Check.Range(age.Value, nameof(age), 0, 100);
        }

        Age = age;
    }

    public void SetAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            Address = null;
            return;
        }

        Check.Length(address, nameof(address), UserConsts.AddressMaxLength);
        Address = address;
    }

    public void SetIntroduction(string? introduction)
    {
        if (string.IsNullOrWhiteSpace(introduction))
        {
            Introduction = null;
            return;
        }

        Check.Length(introduction, nameof(introduction), UserConsts.IntroductionMaxLength);
        Introduction = introduction;
    }
}
