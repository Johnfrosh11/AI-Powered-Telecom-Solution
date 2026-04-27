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

        // Use a stub connection string for migration generation — not executed at design time.
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=NaijaShieldDev;Trusted_Connection=True;",
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
