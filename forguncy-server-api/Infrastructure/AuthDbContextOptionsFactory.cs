using Microsoft.EntityFrameworkCore;

namespace ForguncyServerApi.Infrastructure;

public static class AuthDbContextOptionsFactory
{
    public static DbContextOptions<AuthDbContext> Create(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("A connection string is required.", nameof(connectionString));
        }

        return new DbContextOptionsBuilder<AuthDbContext>()
            .UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString))
            .Options;
    }
}
