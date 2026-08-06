using ForguncyServerApi.Domain;
using SqlSugar;

namespace ForguncyServerApi.Infrastructure;

public sealed class UserRepository : IUserRepository
{
    private readonly Func<SqlSugarClient> _clientFactory;

    public UserRepository(Func<SqlSugarClient> clientFactory)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
    }

    public async Task<AuthUser?> FindByUsernameAsync(string creditCode, CancellationToken cancellationToken)
    {
        if (creditCode is null)
        {
            throw new ArgumentNullException(nameof(creditCode));
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var client = _clientFactory();
        var user = await client.Queryable<AuthUser>()
            .Where(user => user.Username == creditCode)
            .SingleAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return user is not null && string.Equals(user.Username, creditCode, StringComparison.Ordinal)
            ? user
            : null;
    }
}
