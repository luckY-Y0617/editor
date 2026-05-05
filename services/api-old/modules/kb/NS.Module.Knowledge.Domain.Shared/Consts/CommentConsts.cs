namespace NS.Module.Knowledge.Domain.Shared.Consts
{
    /// <summary>
    /// 评论相关通用常量配置。
    /// </summary>
    public static class CommentConsts
    {
        /// <summary>
        /// 评论内容最大长度。
        /// </summary>
        public const int MaxContentLength = 2000;

        /// <summary>
        /// 锚点 BlockId 的最大长度（对应 TipTap block 的 id），
        /// 一般使用短字符串即可。
        /// </summary>
        public const int MaxBlockIdLength = 64;

        /// <summary>
        /// 允许的最大嵌套层级（父评论 -> 子回复）。
        /// 目前前端只做两层（评论 + 回复），这里预留。
        /// </summary>
        public const int MaxReplyDepth = 3;
    }
}