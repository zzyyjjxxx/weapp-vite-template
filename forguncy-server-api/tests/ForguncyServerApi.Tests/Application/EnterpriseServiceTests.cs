using ForguncyServerApi.Application;
using ForguncyServerApi.Domain;
using ForguncyServerApi.Infrastructure;
using Xunit;

namespace ForguncyServerApi.Tests.Application;

public sealed class EnterpriseServiceTests
{
    [Fact]
    public async Task GetProfileAsync_uses_the_authenticated_credit_code_and_returns_the_repository_profile_user_id()
    {
        var repository = new StubEnterpriseRepository(new EnterpriseProfile
        {
            UserId = 42,
            CreditCode = "91330200SYNTHETIC",
            BusinessName = "Synthetic Enterprise",
            CountyName = "Yinzhou",
            Region = "Shounan"
        });
        var service = new EnterpriseService(repository);

        var result = await service.GetProfileAsync(
            new EnterpriseIdentity(7, "91330200SYNTHETIC"),
            CancellationToken.None);

        Assert.Equal("91330200SYNTHETIC", repository.LastCreditCode);
        Assert.NotNull(result);
        Assert.Equal(42, result!.UserId);
        Assert.Equal("Synthetic Enterprise", result.BusinessName);
        Assert.Equal("91330200SYNTHETIC", result.CreditCode);
        Assert.Equal("Yinzhou", result.CountyName);
        Assert.Equal("Shounan", result.Region);
    }

    [Fact]
    public async Task GetProfileAsync_returns_null_when_the_credit_code_has_no_matching_profile()
    {
        var repository = new StubEnterpriseRepository(null);
        var service = new EnterpriseService(repository);

        var result = await service.GetProfileAsync(
            new EnterpriseIdentity(7, "91330200MISSING"),
            CancellationToken.None);

        Assert.Equal("91330200MISSING", repository.LastCreditCode);
        Assert.Null(result);
    }

    private sealed class StubEnterpriseRepository : IEnterpriseRepository
    {
        private readonly EnterpriseProfile? profile;

        public StubEnterpriseRepository(EnterpriseProfile? profile)
        {
            this.profile = profile;
        }

        public string? LastCreditCode { get; private set; }

        public Task<EnterpriseProfile?> FindByCreditCodeAsync(string creditCode, CancellationToken cancellationToken)
        {
            LastCreditCode = creditCode;
            return Task.FromResult(profile is not null && profile.CreditCode == creditCode ? profile : null);
        }
    }
}
