using ForguncyServerApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace ForguncyServerApi.Infrastructure;

public sealed class UserRepository : IUserRepository
{
    private readonly Func<AuthDbContext> _contextFactory;

    public UserRepository(Func<AuthDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<AuthUser?> FindByUsernameAsync(string creditCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(creditCode);

        await using var context = _contextFactory();
        var user = await context.Users.SingleOrDefaultAsync(user => user.Username == creditCode, cancellationToken);
        return user is not null && string.Equals(user.Username, creditCode, StringComparison.Ordinal)
            ? user
            : null;
    }
}
