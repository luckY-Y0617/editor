using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Northstar.Application.Common;

namespace Northstar.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeNorthstarDatabaseAsync(
        this IServiceProvider serviceProvider,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();

        if (dbContext.Database.IsRelational() &&
            configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        else if (!dbContext.Database.IsRelational())
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }

        var seeder = scope.ServiceProvider.GetRequiredService<INorthstarDataSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}

