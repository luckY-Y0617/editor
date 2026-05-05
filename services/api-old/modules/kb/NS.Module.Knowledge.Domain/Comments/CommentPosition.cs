using Volo.Abp;

namespace NS.Module.Knowledge.Domain.Comments;

public sealed class CommentPosition
{
    public int Schema { get; private set; } = 1;

    /// <summary>
    /// range | block
    /// </summary>
    public string Type { get; private set; } = "range";

    public string? BlockId { get; private set; }

    public QuotePosition? Quote { get; private set; }

    public int? Occurrence { get; private set; }

    public PositionHint? Hint { get; private set; }

    private CommentPosition() { }

    public static CommentPosition CreateRange(
        string blockId,
        string exact,
        string? prefix,
        string? suffix,
        int occurrence,
        int? fromInBlock,
        int? toInBlock)
    {
        Check.NotNullOrWhiteSpace(blockId, nameof(blockId));
        Check.NotNullOrWhiteSpace(exact, nameof(exact));

        return new CommentPosition
        {
            Schema = 1,
            Type = "range",
            BlockId = blockId,
            Quote = new QuotePosition(exact, prefix, suffix),
            Occurrence = occurrence,
            Hint = (fromInBlock.HasValue && toInBlock.HasValue)
                ? new PositionHint(fromInBlock.Value, toInBlock.Value)
                : null
        };
    }

    public static CommentPosition CreateBlock(string blockId)
    {
        Check.NotNullOrWhiteSpace(blockId, nameof(blockId));

        return new CommentPosition
        {
            Schema = 1,
            Type = "block",
            BlockId = blockId
        };
    }
}