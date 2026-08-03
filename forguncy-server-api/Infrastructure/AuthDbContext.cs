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
        var isSqlite = Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";
        var user = modelBuilder.Entity<AuthUser>();
        user.ToTable("jwt_users");
        if (Database.ProviderName == "Pomelo.EntityFrameworkCore.MySql")
        {
            user.Metadata.SetAnnotation("MySql:CharSet", "utf8mb4");
            user.Metadata.SetAnnotation("Relational:Collation", "utf8mb4_unicode_ci");
        }

        user.HasKey(entity => entity.Id);
        user.Property(entity => entity.Id)
            .HasColumnName("id")
            .HasColumnType(isSqlite ? "INTEGER" : "BIGINT")
            .ValueGeneratedOnAdd();
        user.Property(entity => entity.Username)
            .HasColumnName("username")
            .HasColumnType(isSqlite ? "TEXT" : "varchar(100)")
            .HasMaxLength(100)
            .IsRequired();
        user.HasIndex(entity => entity.Username).IsUnique();
        user.Property(entity => entity.PasswordHash)
            .HasColumnName("password_hash")
            .HasColumnType(isSqlite ? "TEXT" : "varchar(512)")
            .HasMaxLength(512)
            .IsRequired();
        user.Property(entity => entity.IsEnabled)
            .HasColumnName("is_enabled")
            .HasColumnType(isSqlite ? "INTEGER" : "tinyint(1)")
            .HasDefaultValue(true)
            .ValueGeneratedNever()
            .IsRequired();
        user.Property(entity => entity.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType(isSqlite ? "TEXT" : "datetime(6)")
            .IsRequired();
        user.Property(entity => entity.UpdatedAtUtc)
            .HasColumnName("updated_at")
            .HasColumnType(isSqlite ? "TEXT" : "datetime(6)")
            .IsRequired();
    }
}
