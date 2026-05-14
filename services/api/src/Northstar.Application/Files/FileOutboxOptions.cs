namespace Northstar.Application.Files;

public sealed class FileOutboxOptions
{
    public const string SectionName = "Files:Outbox";

    public bool Enabled { get; init; } = true;
    public int BatchSize { get; init; } = 25;
    public int RetryDelaySeconds { get; init; } = 60;
    public int MaxAttempts { get; init; } = 3;
    public int ScanIntervalSeconds { get; init; } = 60;
}
