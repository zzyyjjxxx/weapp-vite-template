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
        user.ToTable("c_userinfo");
        if (Database.ProviderName == "Pomelo.EntityFrameworkCore.MySql")
        {
            user.Metadata.SetAnnotation("MySql:CharSet", "utf8mb4");
            user.Metadata.SetAnnotation("Relational:Collation", "utf8mb4_unicode_ci");
        }

        user.HasKey(entity => entity.Id);
        user.Property(entity => entity.Id)
            .HasColumnName("id")
            .HasColumnType("int")
            .ValueGeneratedNever();
        user.Property(entity => entity.Username)
            .HasColumnName("creditCode")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired();
        user.Property(entity => entity.PasswordHash)
            .HasColumnName("password")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired();
        user.Property(entity => entity.IsOpen)
            .HasColumnName("isopen")
            .HasColumnType("int")
            .ValueGeneratedNever()
            .IsRequired();
    }
}
