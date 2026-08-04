using ForguncyServerApi.Domain;

namespace ForguncyServerApi.Infrastructure;

public interface IUserRepository
{
    Task<AuthUser?> FindByUsernameAsync(string creditCode, CancellationToken cancellationToken);
}
