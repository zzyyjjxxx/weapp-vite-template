using ForguncyServerApi.Application;
using ForguncyServerApi.Configuration;
using ForguncyServerApi.Domain;
using ForguncyServerApi.Infrastructure;
using ForguncyServerApi.Security;
using GrapeCity.Forguncy.ServerApi;
using SqlSugar;
using System.Net.Http;

namespace ForguncyServerApi.Api;

public sealed class EnterpriseCompositionRoot
{
    private static readonly RetryableAsyncCache<EnterpriseCompositionRoot> SharedCompositionCache = new();

    private EnterpriseCompositionRoot(
        AuthService authService,
        EnterpriseService enterpriseService,
        LandDemandService landDemandService,
        IJwtTokenService tokens,
        Func<SqlSugarClient> clientFactory,
        Func<IDataAccess, SmsVerificationService>? smsVerificationServiceFactory)
    {
        AuthService = authService;
        EnterpriseService = enterpriseService;
        LandDemandService = landDemandService;
        Tokens = tokens;
        ClientFactory = clientFactory;
        this.smsVerificationServiceFactory = smsVerificationServiceFactory;
    }

    private readonly Func<IDataAccess, SmsVerificationService>? smsVerificationServiceFactory;

    public AuthService AuthService { get; }

    public EnterpriseService EnterpriseService { get; }

    public LandDemandService LandDemandService { get; }

    public IJwtTokenService Tokens { get; }

    public Func<SqlSugarClient> ClientFactory { get; }

    public SmsVerificationService CreateSmsVerificationService(IDataAccess dataAccess)
    {
        if (dataAccess is null)
        {
            throw new ArgumentNullException(nameof(dataAccess));
        }

        return smsVerificationServiceFactory is not null
            ? smsVerificationServiceFactory(dataAccess)
            : throw new InvalidOperationException("SMS verification is not configured.");
    }

    public static Task<EnterpriseCompositionRoot> GetOrCreateAsync(
        IDataAccess dataAccess,
        CancellationToken cancellationToken)
    {
        if (dataAccess is null)
        {
            throw new ArgumentNullException(nameof(dataAccess));
        }

        return SharedCompositionCache.GetOrCreateAsync(
            () => CreateAsync(dataAccess, CancellationToken.None),
            cancellationToken);
    }

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
        var authenticationClient = new HttpSmsAuthenticationClient(new HttpClient());
        var smsGateway = new HttpSmsGateway(new HttpClient());
        Func<IDataAccess, SmsVerificationService> smsVerificationServiceFactory = currentDataAccess =>
            new SmsVerificationService(
                new ForguncyConfigValueStore(currentDataAccess),
                new ForguncySmsVerificationRepository(currentDataAccess),
                new ForguncyMessageLogRepository(currentDataAccess),
                authenticationClient,
                smsGateway,
                new RandomVerificationCodeGenerator(),
                new SequentialTransactionIdGenerator(),
                () => DateTimeOffset.Now);

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
                clientFactory,
                smsVerificationServiceFactory));
    }

    public Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
        AuthService.LoginAsync(request, cancellationToken);

    public Task<RefreshResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
        AuthService.RefreshAsync(refreshToken, cancellationToken);

    public Task<EnterpriseProfile?> GetInfoAsync(EnterpriseIdentity identity, CancellationToken cancellationToken) =>
        EnterpriseService.GetProfileAsync(identity, cancellationToken);
}
