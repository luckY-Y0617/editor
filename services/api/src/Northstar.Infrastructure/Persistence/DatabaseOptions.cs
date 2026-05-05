namespace Northstar.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public const string ConnectionStringName = "Northstar";

    public string Provider { get; init; } = "PostgreSQL";
    public string? ConnectionString { get; init; }
    public int CommandTimeoutSeconds { get; init; } = 30;
}

