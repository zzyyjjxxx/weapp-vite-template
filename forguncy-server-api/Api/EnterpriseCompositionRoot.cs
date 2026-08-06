using ForguncyServerApi.Application;
using ForguncyServerApi.Configuration;
using ForguncyServerApi.Domain;
using ForguncyServerApi.Infrastructure;
using ForguncyServerApi.Security;
using GrapeCity.Forguncy.ServerApi;
using SqlSugar;

namespace ForguncyServerApi.Api;

public sealed class EnterpriseCompositionRoot
{
    private EnterpriseCompositionRoot(
        AuthService authService,
        EnterpriseService enterpriseService,
        LandDemandService landDemandService,
        IJwtTokenService tokens,
        Func<SqlSugarClient> clientFactory)
    {
        AuthService = authService;
        EnterpriseService = enterpriseService;
        LandDemandService = landDemandService;
        Tokens = tokens;
        ClientFactory = clientFactory;
    }

    public AuthService AuthService { get; }

    public EnterpriseService EnterpriseService { get; }

    public LandDemandService LandDemandService { get; }

    public IJwtTokenService Tokens { get; }

    public Func<SqlSugarClient> ClientFactory { get; }

    public static Task<EnterpriseCompositionRoot> CreateAsync(
        IDataAccess dataAccess,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connectionString = ForguncyConfigConnectionStringReader.ReadRequired(dataAccess);
        var options = AuthOptions.From(ForguncyJwtConfigurationReader.ReadOrCreate(dataAccess));
        Func<SqlSugarClient> clientFactory = () => AuthSqlSugarClientFactory.Create(connectionString);

        var tokens = new JwtTokenService(options);
        var enterpriseRepository = new EnterpriseRepository(clientFactory);
        var enterpriseService = new EnterpriseService(enterpriseRepository);

        return Task.FromResult(
            new EnterpriseCompositionRoot(
                new AuthService(
                    new UserRepository(clientFactory),
                    new PasswordHasher(),
                    tokens,
                    options.JwtLifetime,
                    options.JwtRefreshLifetime),
                enterpriseService,
                new LandDemandService(
                    enterpriseService,
                    new LandDemandRepository(clientFactory),
                    () => DateTimeOffset.Now),
                tokens,
                clientFactory));
    }

    public Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
        AuthService.LoginAsync(request, cancellationToken);

    public Task<RefreshResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
        AuthService.RefreshAsync(refreshToken, cancellationToken);

    public Task<EnterpriseProfile?> GetInfoAsync(EnterpriseIdentity identity, CancellationToken cancellationToken) =>
        EnterpriseService.GetProfileAsync(identity, cancellationToken);
}
