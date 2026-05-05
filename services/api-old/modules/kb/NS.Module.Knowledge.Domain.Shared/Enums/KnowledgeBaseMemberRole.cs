using System.Runtime.Serialization;

namespace NS.Module.Knowledge.Domain.Shared.Enums
{
    public enum KnowledgeBaseMemberRole
    {
        [EnumMember(Value = "Owner")]
        Owner = 0,
        
        [EnumMember(Value = "Admin")]
        Admin = 1,
        
        [EnumMember(Value = "Editor")]
        Editor = 2,
        
        [EnumMember(Value = "Viewer")]
        Viewer = 3
    }
}