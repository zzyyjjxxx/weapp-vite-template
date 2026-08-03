using ForguncyServerApi.Domain;

namespace ForguncyServerApi.Infrastructure;

public interface IUserRepository
{
    Task<AuthUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken);
    Task AddAsync(AuthUser user, CancellationToken cancellationToken);
}
