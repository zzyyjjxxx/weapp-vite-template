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

    public async Task<AuthUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(username);

        await using var context = _contextFactory();
        return await context.Users.SingleOrDefaultAsync(user => user.Username == username, cancellationToken);
    }

    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(username);

        await using var context = _contextFactory();
        return await context.Users.AnyAsync(user => user.Username == username, cancellationToken);
    }

    public async Task AddAsync(AuthUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var context = _contextFactory();
        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
