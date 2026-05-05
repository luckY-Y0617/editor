namespace NS.Module.Knowledge.Domain.Shared.Enums
{
    /// <summary>
    /// 文档版本产生的来源。
    /// </summary>
    public enum DocumentVersionSource : byte
    {
        /// <summary>
        /// 自动保存：只更新当前内容，不写入版本表。
        /// </summary>
        AutoSave = 0,

        /// <summary>
        /// 手动保存版本：用户点击“保存版本”等显式操作。
        /// </summary>
        ManualSave = 1,

        /// <summary>
        /// 初始版本：文档首次创建时的内容快照。
        /// （如果你不想单独区分，也可以暂时不使用）
        /// </summary>
        Initial = 2,

        /// <summary>
        /// 从历史版本恢复而产生的新版本。
        /// </summary>
        Restore = 3
    }
}