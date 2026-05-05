namespace NS.Framework.SqlSugar.Abstractions.Migrations;

public sealed record MigrationRequest(
    string Scope,
    MigrationFlags Flags = MigrationFlags.EnsureDatabaseCreated)
{
    public bool EnsureDatabaseCreated => Flags.HasFlag(MigrationFlags.EnsureDatabaseCreated);
    public bool UseTransaction => Flags.HasFlag(MigrationFlags.UseTransaction);
}

public sealed record MigrationResult(
    bool Succeeded,
    int AppliedCount,
    int SkippedCount,
    Exception? Error = null)
{
    public static MigrationResult Success(int appliedCount, int skippedCount)
        => new(true, appliedCount, skippedCount);

    public static MigrationResult Failure(Exception ex, int appliedCount, int skippedCount)
        => new(false, appliedCount, skippedCount, ex);
}

[Flags]
public enum MigrationFlags
{
    None = 0,
    EnsureDatabaseCreated = 1,
    UseTransaction = 2
}

public static class MigrationScopes
{
    public const string Host = "Host";
    public const string Tenant = "Tenant";
    public const string Both = "Both";
}




