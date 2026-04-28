using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using NaijaShield.Application.Common.Interfaces;

namespace NaijaShield.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core tools (migrations) when
/// ICurrentUserService is not available from the DI container.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Prefer env var or fall back to Azure dev DB for migration tooling.
        var connString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=tcp:ai-telecom-solution.database.windows.net,1433;Initial Catalog=ai-telecom-dev-db;Persist Security Info=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default;";

        optionsBuilder.UseSqlServer(
            connString,
            sql =>
            {
                sql.EnableRetryOnFailure(3);
                sql.MigrationsAssembly("NaijaShield.Infrastructure");
            });

        return new AppDbContext(optionsBuilder.Options, new DesignTimeCurrentUser());
    }

    /// <summary>Stub ICurrentUserService for EF design-time tools.</summary>
    private sealed class DesignTimeCurrentUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? TenantId => null;
        public string? Email => null;
        public IReadOnlyList<string> Roles => [];
        public IReadOnlyList<string> Permissions => [];
        public bool IsAuthenticated => false;
        public string? IpAddress => null;
        public bool HasPermission(string permission) => false;
    }
}
