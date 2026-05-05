namespace NS.Module.Knowledge.Domain.Shared.Authorization;

public static class KnowledgePermissions
{
    /// <summary>
    /// 用于前端构建权限树的分组名称
    /// </summary>
    public const string GroupName = "knowledge";

    // ======================
    // 知识库（KnowledgeBase）
    // ======================
    public static class KnowledgeBase
    {
        /// <summary>
        /// 查看知识库列表、进入某个知识库（权限 + 可见性判断）
        /// </summary>
        public const string View = "kb.base.view";

        /// <summary>
        /// 创建知识库
        /// </summary>
        public const string Create = "kb.base.create";

        /// <summary>
        /// 编辑知识库信息（名称、描述、可见性、团队信息等）
        /// </summary>
        public const string Manage = "kb.base.manage";

        /// <summary>
        /// 删除知识库（危险操作，需高权限）
        /// </summary>
        public const string Delete = "kb.base.delete";

        /// <summary>
        /// 成员管理（增删成员、调整成员角色）
        /// </summary>
        public const string MemberManage = "kb.base.member.manage";
    }

    // ======================
    // 文档（Document）
    // ======================
    public static class Document
    {
        /// <summary>查看文档内容</summary>
        public const string View = "kb.doc.view";

        /// <summary>创建文档</summary>
        public const string Create = "kb.doc.create";

        /// <summary>编辑文档</summary>
        public const string Edit = "kb.doc.edit";

        /// <summary>删除文档</summary>
        public const string Delete = "kb.doc.delete";

        /// <summary>移动文档（树结构变化）</summary>
        public const string Move = "kb.doc.move";
    }

    // ======================
    // 标签（Tag）
    // ======================
    public static class Tag
    {
        /// <summary>查看全部标签</summary>
        public const string View = "kb.tag.view";

        /// <summary>创建/修改/删除标签</summary>
        public const string Edit = "kb.tag.edit";
    }

    // ======================
    // 文档版本管理（Version）
    // ======================
    public static class Version
    {
        /// <summary>查看文档历史版本</summary>
        public const string View = "kb.version.view";

        /// <summary>恢复到某个历史版本</summary>
        public const string Restore = "kb.version.restore";
    }
    
    // ======================
// 评论（Comment）
// ======================
    public static class Comment
    {
        /// <summary>
        /// 查看评论（包括加载评论列表、展开回复）
        /// </summary>
        public const string View = "kb.comment.view";

        /// <summary>
        /// 发表评论 / 回复评论
        /// </summary>
        public const string Create = "kb.comment.create";

        /// <summary>
        /// 编辑自己发表的评论
        /// </summary>
        public const string Edit = "kb.comment.edit";

        /// <summary>
        /// 删除评论（通常仅限本人或管理员）
        /// </summary>
        public const string Delete = "kb.comment.delete";

        /// <summary>
        /// 点赞 / 取消点赞
        /// </summary>
        public const string Like = "kb.comment.like";
    }

}
