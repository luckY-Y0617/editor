namespace NS.Module.Identity.Domain.Shared.Enums
{
    /// <summary>
    /// - 仅用于团队本身的管理（增删成员、改名、解散等）
    /// </summary>
    public enum TeamMemberRole : byte
    {
        /// <summary>
        /// 团队拥有者：
        /// - 创建人或被转移所有权的人
        /// - 拥有对该团队的所有管理权限（包括解散团队）
        /// </summary>
        Owner = 0,

        /// <summary>
        /// 管理员：
        /// - 可以管理团队成员（邀请/移除）
        /// - 可以修改团队信息（名称、描述、头像等）
        /// - 不能解散团队、不能转移 Owner（可根据业务需要放开）
        /// </summary>
        Admin = 1,

        /// <summary>
        /// 普通成员：
        /// - 属于该团队
        /// - 享受“Teams 可见”的自动授权（例如可访问 Teams 可见的 KB）
        /// - 默认无团队管理权限
        /// </summary>
        Member = 2
    }
}