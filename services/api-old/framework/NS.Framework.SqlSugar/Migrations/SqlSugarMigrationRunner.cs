using System.Reflection;
using NS.Framework.SqlSugar.Abstractions;
using NS.Framework.SqlSugar.Abstractions.Migrations;
using Microsoft.Extensions.Logging;

namespace NS.Framework.SqlSugar.Migrations;

internal sealed class SqlSugarMigrationRunner : IMigrationRunner
{
    private readonly IEnumerable<IMigrationContributor> _contributors;
    private readonly ISqlSugarDbContextProvider<SqlSugarDbContext> _dbContextProvider;
    private readonly ILogger<SqlSugarMigrationRunner> _logger;

    public SqlSugarMigrationRunner(
        IEnumerable<IMigrationContributor> contributors,
        ISqlSugarDbContextProvider<SqlSugarDbContext> dbContextProvider,
        ILogger<SqlSugarMigrationRunner> logger)
    {
        _contributors = contributors;
        _dbContextProvider = dbContextProvider;
        _logger = logger;
    }

    public async Task<MigrationResult> MigrateAsync(MigrationRequest request, CancellationToken cancellationToken = default)
    {
        var appliedCount = 0;

        try
        {
            var db = (await _dbContextProvider.GetDbContextAsync()).Client;
            db.DbMaintenance.CreateDatabase();

            var targetScope = request.Scope?.Trim();
            if (string.IsNullOrEmpty(targetScope) || targetScope.Equals(MigrationScopes.Both, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Migration scope 'Both' 不应在 Runner 内部直接执行。请调用方分别对 Host/Tenant 执行。");

            var migrations = _contributors
                .Select(c => (Contributor: c, Attr: c.GetType().GetCustomAttribute<MigrationAttribute>()))
                .Where(x =>
                {
                    var scope = x.Attr?.Scope;
                    // 没有标注或标注为 Both 的，匹配所有目标
                    if (string.IsNullOrWhiteSpace(scope) || scope.Equals(MigrationScopes.Both, StringComparison.OrdinalIgnoreCase))
                        return true;
                    // 否则精确匹配
                    return scope.Equals(targetScope, StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(x => x.Attr?.Order ?? 0)
                .ThenBy(x => x.Contributor.Id, StringComparer.Ordinal);

            foreach (var (contributor, _) in migrations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation(
                    "执行 Migration: {Id} | {Desc} | Repeatable={Repeatable}",
                    contributor.Id,
                    contributor.Description,
                    contributor.IsRepeatable);

                await contributor.ExecuteAsync(db, cancellationToken);
                appliedCount++;
            }

            return MigrationResult.Success(appliedCount, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database migration failed.");
            return MigrationResult.Failure(ex, appliedCount, 0);
        }
    }
}
