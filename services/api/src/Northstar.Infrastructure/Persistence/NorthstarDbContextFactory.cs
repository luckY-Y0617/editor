using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Northstar.Infrastructure.Persistence;

public sealed class NorthstarDbContextFactory : IDesignTimeDbContextFactory<NorthstarDbContext>
{
    public NorthstarDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("NORTHSTAR_DATABASE_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=northstar;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<NorthstarDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(NorthstarDbContext).Assembly.FullName))
            .Options;

        return new NorthstarDbContext(options);
    }
}

