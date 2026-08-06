using ForguncyServerApi.Domain;

namespace ForguncyServerApi.Infrastructure;

public interface IEnterpriseRepository
{
    Task<EnterpriseProfile?> FindByCreditCodeAsync(string creditCode, CancellationToken cancellationToken);
}
