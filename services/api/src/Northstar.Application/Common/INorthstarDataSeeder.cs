namespace Northstar.Application.Common;

public interface INorthstarDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

