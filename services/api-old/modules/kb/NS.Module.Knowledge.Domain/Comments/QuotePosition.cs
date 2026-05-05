namespace NS.Module.Knowledge.Domain.Comments;

public sealed class QuotePosition
{
    public string Exact { get; private set; }
    public string? Prefix { get; private set; }
    public string? Suffix { get; private set; }

    private QuotePosition() { }

    public QuotePosition(string exact, string? prefix, string? suffix)
    {
        Exact = exact;
        Prefix = prefix;
        Suffix = suffix;
    }
}