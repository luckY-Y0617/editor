using Microsoft.EntityFrameworkCore;
using Northstar.Application.Common;
using Northstar.Contracts.Common;
using Npgsql;

namespace Northstar.Infrastructure.Persistence;

public sealed class EfTransactionRunner : ITransactionRunner
{
    private const string UniqueViolation = "23505";
    private readonly NorthstarDbContext _dbContext;

    public EfTransactionRunner(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (!_dbContext.Database.IsRelational())
        {
            return await operation(cancellationToken);
        }

        try
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw new ApplicationErrorException(ErrorCodes.Conflict, "A conflicting record already exists.");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: UniqueViolation };
    }
}
