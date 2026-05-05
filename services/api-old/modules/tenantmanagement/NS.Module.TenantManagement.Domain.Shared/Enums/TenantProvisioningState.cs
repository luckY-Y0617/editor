using System.Runtime.Serialization;

namespace NS.Module.TenantManagement.Domain.Shared.Enums;

/// <summary>
/// 租户初始化状态
/// </summary>
public enum TenantProvisioningState
{
    [EnumMember(Value = "NotReady")]
    NotReady = 0,
    
    [EnumMember(Value = "Provisioning")]
    Provisioning = 1,
    
    [EnumMember(Value = "Ready")]
    Ready = 2,
    
    [EnumMember(Value = "Failed")]
    Failed = 3
}