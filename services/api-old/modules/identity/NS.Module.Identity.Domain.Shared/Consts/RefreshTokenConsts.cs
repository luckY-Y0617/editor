namespace NS.Module.Identity.Domain.Shared.Consts;

public static class RefreshTokenConsts
{
    // token hash：建议存 HMAC-SHA256 的 hex（64）或 base64url（43~44）
    // 这里留更大余量，方便未来改算法/编码方式不动表结构
    public const int TokenHashMaxLength = 128;

    // 链路/会话标识：建议用 ULID/UUID/base64url
    public const int SessionIdMaxLength = 64;

    // 客户端/设备信息（可选，但强烈建议保留以便风控与会话管理）
    public const int ClientIdMaxLength = 64;
    public const int DeviceIdMaxLength = 128;

    // 审计字段（尽量留足）
    public const int IpAddressMaxLength = 64;
    public const int UserAgentMaxLength = 512;

    public const int RevokeReasonMaxLength = 256;

    // 如你希望把“指纹/设备指纹”落库，可用这个
    public const int FingerprintMaxLength = 256;
}