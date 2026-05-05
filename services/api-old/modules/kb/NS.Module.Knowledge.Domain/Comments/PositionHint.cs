namespace NS.Module.Knowledge.Domain.Comments;

/// <summary>
/// 前端重定位 hint（非语义，不保证正确）
/// </summary>
public sealed class PositionHint
{
    public int FromInBlock { get; private set; }
    public int ToInBlock { get; private set; }

    private PositionHint() { }

    public PositionHint(int fromInBlock, int toInBlock)
    {
        FromInBlock = fromInBlock;
        ToInBlock = toInBlock;
    }
}