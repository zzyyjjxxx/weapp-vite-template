using ForguncyServerApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace ForguncyServerApi.Infrastructure;

public sealed class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuthUser> Users => Set<AuthUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<AuthUser>();
        user.ToTable("jwt_users");
        user.HasCharSet("utf8mb4").UseCollation("utf8mb4_unicode_ci");

        user.HasKey(entity => entity.Id);
        user.Property(entity => entity.Id)
            .HasColumnName("id")
            .HasColumnType(Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite" ? "INTEGER" : "BIGINT")
            .ValueGeneratedOnAdd();
        user.Property(entity => entity.Username)
            .HasColumnName("username")
            .HasMaxLength(100)
            .IsRequired();
        user.HasIndex(entity => entity.Username).IsUnique();
        user.Property(entity => entity.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512)
            .IsRequired();
        user.Property(entity => entity.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();
        user.Property(entity => entity.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("datetime(6)")
            .IsRequired();
        user.Property(entity => entity.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("datetime(6)")
            .IsRequired();
    }
}
