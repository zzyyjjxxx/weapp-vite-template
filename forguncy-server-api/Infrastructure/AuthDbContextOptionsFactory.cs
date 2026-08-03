using ForguncyServerApi.Configuration;
using Microsoft.EntityFrameworkCore;

namespace ForguncyServerApi.Infrastructure;

public static class AuthDbContextOptionsFactory
{
    public static DbContextOptions<AuthDbContext> Create(AuthOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new DbContextOptionsBuilder<AuthDbContext>()
            .UseMySql(
                options.MySqlConnectionString,
                ServerVersion.AutoDetect(options.MySqlConnectionString))
            .Options;
    }
}
