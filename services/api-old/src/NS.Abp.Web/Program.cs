namespace NS.Abp.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile(
                "appsettings.Local.json",
                optional: true,
                reloadOnChange: true);
        }

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddAuthorization();

        builder.Host.UseAutofac();
        await builder.Services.AddApplicationAsync<ClayMoAbpWebModule>();

        var app = builder.Build();

        await app.InitializeApplicationAsync();
        
        app.MapControllers();
        
        await app.RunAsync();
    }
}