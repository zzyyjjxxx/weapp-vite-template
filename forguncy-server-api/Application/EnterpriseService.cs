using ForguncyServerApi.Domain;
using ForguncyServerApi.Infrastructure;

namespace ForguncyServerApi.Application;

public sealed class EnterpriseService
{
    private readonly IEnterpriseRepository repository;

    public EnterpriseService(IEnterpriseRepository repository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<EnterpriseProfile?> GetProfileAsync(EnterpriseIdentity identity, CancellationToken cancellationToken)
    {
        if (identity is null)
        {
            throw new ArgumentNullException(nameof(identity));
        }

        if (string.IsNullOrWhiteSpace(identity.CreditCode))
        {
            throw new ArgumentException("Enterprise identity credit code is required.", nameof(identity));
        }

        var creditCode = identity.CreditCode.Trim();
        cancellationToken.ThrowIfCancellationRequested();
        var profile = await repository.FindByCreditCodeAsync(creditCode, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return profile;
    }
}
