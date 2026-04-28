using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaijaShield.Domain.Aggregates.Identity;
using NaijaShield.Domain.Aggregates.Tenants;

namespace NaijaShield.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("Tenants");
        b.HasKey(e => e.Id);
        b.Property(e => e.Name).HasMaxLength(200).IsRequired();
        b.Property(e => e.Slug).HasMaxLength(100).IsRequired();
        b.HasIndex(e => e.Slug).IsUnique();
        b.Property(e => e.Plan).HasConversion<string>().HasMaxLength(50);
        b.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
        b.Property(e => e.BlockingConfidenceThreshold).HasPrecision(5, 4);
    }
}

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("Users");
        b.HasKey(e => e.Id);
        b.Property(e => e.Email).HasMaxLength(320).IsRequired();
        b.HasIndex(e => e.Email).IsUnique();
        b.Property(e => e.FullName).HasMaxLength(200);
        b.Property(e => e.PasswordHash).HasMaxLength(512);
        b.HasIndex(e => e.EntraObjectId);

        b.HasMany(e => e.UserRoles).WithOne()
            .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);

        b.OwnsOne(e => e.NotificationPrefs, nb =>
        {
            nb.ToTable("UserNotificationPreferences");
            nb.WithOwner().HasForeignKey("UserId");
            nb.HasKey("UserId");
        });
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> b)
    {
        b.ToTable("UserRoles");
        b.HasKey(e => new { e.UserId, e.RoleId });
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("Roles");
        b.HasKey(e => e.Id);
        b.Property(e => e.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();

        b.HasMany(e => e.RolePermissions).WithOne()
            .HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<PermissionEntry>
{
    public void Configure(EntityTypeBuilder<PermissionEntry> b)
    {
        b.ToTable("Permissions");
        b.HasKey(e => e.Id);
        b.Property(e => e.Code).HasMaxLength(100).IsRequired();
        b.HasIndex(e => e.Code).IsUnique();
    }
}

public class RolePermissionAssignmentConfiguration : IEntityTypeConfiguration<RolePermissionAssignment>
{
    public void Configure(EntityTypeBuilder<RolePermissionAssignment> b)
    {
        b.ToTable("RolePermissions");
        b.HasKey(e => new { e.RoleId, e.PermissionId });
    }
}
