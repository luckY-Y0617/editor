namespace NS.Module.Identity.Domain.Shared.Enums;

public enum LoginTypeEnum : byte
{
    UserNamePassword = 0,
    PhoneVerificationCode = 1,
    EmailVerificationCode = 2,
    External = 3
}


