namespace NS.Module.Knowledge.Domain.Shared
{
    /// <summary>
    /// 评论领域错误码。
    /// </summary>
    public static class KnowledgeCommentErrorCodes
    {
        /// <summary>
        /// 文档不存在。
        /// </summary>
        public const string DocumentNotFound = "Knowledge:Comments:DocumentNotFound";

        /// <summary>
        /// 评论不存在。
        /// </summary>
        public const string CommentNotFound = "Knowledge:Comments:CommentNotFound";

        /// <summary>
        /// 评论不属于指定文档。
        /// </summary>
        public const string CommentNotBelongToDocument = "Knowledge:Comments:CommentNotBelongToDocument";

        /// <summary>
        /// 父评论不存在。
        /// </summary>
        public const string ParentCommentNotFound = "Knowledge:Comments:ParentCommentNotFound";

        /// <summary>
        /// 父评论不属于同一文档。
        /// </summary>
        public const string ParentCommentMismatchDocument = "Knowledge:Comments:ParentCommentMismatchDocument";

        /// <summary>
        /// 当前用户无权操作该评论（删除 / 编辑等）。
        /// </summary>
        public const string NoPermissionToModifyComment = "Knowledge:Comments:NoPermissionToModifyComment";
    }
}