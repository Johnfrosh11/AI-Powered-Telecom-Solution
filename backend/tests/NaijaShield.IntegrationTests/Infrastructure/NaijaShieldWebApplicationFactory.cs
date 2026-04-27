using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NaijaShield.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace NaijaShield.IntegrationTests.Infrastructure;

/// <summary>
/// Spins up a real SQL Server via Testcontainers and wires it into the
/// ASP.NET Core test host.  Shared across all tests in the same collection.
/// </summary>
public class NaijaShieldWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("NaijaShield@TestPass1")
        .WithCleanUp(true)
        .Build();

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        Client = CreateClient();
    }

    public new async Task DisposeAsync()
    {
        await _sqlContainer.StopAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace real DB with Testcontainers SQL Server
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    _sqlContainer.GetConnectionString(),
                    sql => sql.MigrationsAssembly("NaijaShield.Infrastructure")));

            // Run migrations so the schema exists
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        });
    }
}
