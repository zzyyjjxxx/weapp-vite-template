using System.Reflection;
using System.Text;
using ForguncyServerApi.Api;
using ForguncyServerApi.Application;
using ForguncyServerApi.Configuration;
using ForguncyServerApi.Domain;
using ForguncyServerApi.Infrastructure;
using ForguncyServerApi.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SqlSugar;
using Xunit;

namespace ForguncyServerApi.Tests.Api;

public sealed class LandDemandApiSurfaceTests
{
    static LandDemandApiSurfaceTests()
    {
        AppDomain.CurrentDomain.AssemblyResolve += ResolveForguncyServerApi;
    }

    [Fact]
    public void LandDemandApi_exposes_only_the_three_parameterless_task_handlers()
    {
        var type = typeof(LandDemandApi);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.Equal("GrapeCity.Forguncy.ServerApi.ForguncyApi", type.BaseType?.FullName);
        Assert.Equal(3, methods.Length);
        AssertHandler(methods, "GetLandDemand", "GrapeCity.Forguncy.ServerApi.GetAttribute");
        AssertHandler(methods, "AddLandDemand", "GrapeCity.Forguncy.ServerApi.PostAttribute");
        AssertHandler(methods, "UpdateLandDemand", "GrapeCity.Forguncy.ServerApi.PostAttribute");
        Assert.Single(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single().GetParameters());
    }

    [Fact]
    public async Task GetLandDemand_returns_only_the_filing_whitelist_and_update_time()
    {
        var record = SyntheticRecord();
        var repository = new StubLandDemandRepository { Existing = record };
        var response = await InvokeAsync("GetLandDemand", repository);

        Assert.Equal(200, response.StatusCode);
        var json = JObject.Parse(response.Body);
        var expected = new[]
        {
            "area", "building_area", "businessname", "contact", "county", "creditcode",
            "deploy_height", "deploy_landtype", "deploy_park", "deploy_weight", "expect_park",
            "expect_time", "financing_money", "financing_time", "futureindustry", "investment",
            "is_deploy", "is_financing", "is_specialuse", "keyindustry", "landusedemand", "office",
            "phone", "pred_rdex", "pred_tax", "pred_unitenergy", "pred_ys", "project_hydm",
            "projectdata", "region", "updatetime"
        };
        Assert.Equal(expected, json.Properties().Select(property => property.Name).OrderBy(name => name));
        Assert.Equal("Synthetic project", json["projectdata"]?.Value<string>());
        Assert.Equal("2026-08-06 12:34:56", json["updatetime"]?.Value<string>());
        Assert.Null(json["id"]);
        Assert.Null(json["updateuser"]);
        Assert.Null(json["region_remark"]);
        Assert.Null(json["county_isrecommend"]);
    }

    [Fact]
    public async Task GetLandDemand_maps_missing_enterprise_and_record_to_distinct_404_errors()
    {
        var missingEnterprise = await InvokeAsync(
            "GetLandDemand",
            new StubLandDemandRepository(),
            enterpriseExists: false);
        var missingRecord = await InvokeAsync("GetLandDemand", new StubLandDemandRepository());

        AssertResponse(missingEnterprise, 404, "enterprise_not_found");
        AssertResponse(missingRecord, 404, "land_demand_not_found");
    }

    [Fact]
    public async Task AddLandDemand_maps_duplicate_record_to_409()
    {
        var repository = new StubLandDemandRepository { Existing = SyntheticRecord() };

        var response = await InvokeAsync("AddLandDemand", repository, "{\"landusedemand\":\"2\"}");

        AssertResponse(response, 409, "land_demand_exists");
    }

    [Fact]
    public async Task AddLandDemand_maps_invalid_input_to_400()
    {
        var response = await InvokeAsync(
            "AddLandDemand",
            new StubLandDemandRepository(),
            "{\"landusedemand\":\"3\"}");

        AssertResponse(response, 400, "invalid_request");
    }

    [Fact]
    public async Task UpdateLandDemand_maps_missing_record_to_404()
    {
        var response = await InvokeAsync(
            "UpdateLandDemand",
            new StubLandDemandRepository(),
            "{\"landusedemand\":\"2\"}");

        AssertResponse(response, 404, "land_demand_not_found");
    }

    [Fact]
    public async Task AddLandDemand_returns_the_fixed_response_after_successful_insert()
    {
        var response = await InvokeAsync(
            "AddLandDemand",
            new StubLandDemandRepository(),
            "{\"projectdata\":\"new filing\",\"landusedemand\":\"2\"}");

        Assert.Equal(200, response.StatusCode);
        var json = JObject.Parse(response.Body);
        Assert.Equal("new filing", json["projectdata"]?.Value<string>());
        Assert.Equal("91330200SYNTHETIC", json["creditcode"]?.Value<string>());
        Assert.Equal("2026-08-06 00:00:00", json["updatetime"]?.Value<string>());
        Assert.Equal(31, json.Properties().Count());
    }

    [Fact]
    public async Task AddLandDemand_authenticates_before_reading_the_body_and_rejects_refresh_tokens()
    {
        var tokens = TestJwtTokenService();
        var context = CreateApiContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";
        context.Request.Headers["Authorization"] =
            $"Bearer {tokens.CreateRefreshToken(new AuthUser { Id = 7, Username = "91330200SYNTHETIC" })}";
        context.Request.Body = new ThrowingReadStream(new InvalidOperationException("body-must-not-be-read"));

        using var factoryOverride = PushCompositionRootFactoryOverride(
            _ => Task.FromResult(CreateTestCompositionRoot(tokens, true, new StubLandDemandRepository())));
        await InvokeHandlerAsync("AddLandDemand", context);

        var response = await ReadResponseAsync(context);
        AssertResponse(response, 401, "invalid_token");
    }

    [Fact]
    public async Task UpdateLandDemand_rejects_identity_field_injection_as_invalid_request()
    {
        var response = await InvokeAsync(
            "UpdateLandDemand",
            new StubLandDemandRepository { Existing = SyntheticRecord() },
            "{\"creditcode\":\"attacker\",\"landusedemand\":\"2\"}");

        AssertResponse(response, 400, "invalid_request");
    }

    [Fact]
    public async Task GetLandDemand_returns_fixed_server_error_and_sanitized_diagnostic()
    {
        const string secret = "password=synthetic-secret";
        var loggerFactory = new CapturingLoggerFactory();
        var repository = new StubLandDemandRepository
        {
            FindException = new InvalidOperationException(secret)
        };

        var response = await InvokeAsync(
            "GetLandDemand",
            repository,
            loggerFactory: loggerFactory);

        AssertResponse(response, 500, "server_error");
        var entry = Assert.Single(loggerFactory.Logger.Entries);
        Assert.Equal(LogLevel.Error, entry.LogLevel);
        Assert.Equal("LandDemandGetUnexpectedFailure", entry.EventId.Name);
        Assert.Null(entry.Exception);
        var diagnostic = string.Join(
            "|",
            entry.Message,
            string.Join("|", entry.State.Select(field => $"{field.Key}={field.Value}")));
        Assert.DoesNotContain(secret, diagnostic);
        Assert.DoesNotContain("password", diagnostic.ToLowerInvariant());
        Assert.Contains("land_demand.get", diagnostic);
        Assert.Contains(nameof(InvalidOperationException), diagnostic);
    }

    [Fact]
    public async Task GetLandDemand_propagates_request_cancellation()
    {
        var context = CreateApiContext();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        context.RequestAborted = cancellation.Token;

        using var factoryOverride = PushCompositionRootFactoryOverride(
            _ => Task.FromResult(CreateTestCompositionRoot(
                TestJwtTokenService(),
                true,
                new StubLandDemandRepository())));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => InvokeHandlerAsync("GetLandDemand", context));
    }

    private static void AssertHandler(IEnumerable<MethodInfo> methods, string name, string attributeName)
    {
        var method = Assert.Single(methods, candidate => candidate.Name == name);
        Assert.Empty(method.GetParameters());
        Assert.Equal(typeof(Task), method.ReturnType);
        Assert.Contains(method.GetCustomAttributes(false), attribute => attribute.GetType().FullName == attributeName);
    }

    private static async Task<ApiResult> InvokeAsync(
        string handler,
        StubLandDemandRepository repository,
        string? body = null,
        bool enterpriseExists = true,
        ILoggerFactory? loggerFactory = null)
    {
        var tokens = TestJwtTokenService();
        var context = CreateApiContext(loggerFactory);
        context.Request.Method = body is null ? "GET" : "POST";
        context.Request.Headers["Authorization"] =
            $"Bearer {tokens.CreateToken(new AuthUser { Id = 7, Username = "91330200SYNTHETIC" })}";
        if (body is not null)
        {
            context.Request.ContentType = "application/json";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        }

        using var factoryOverride = PushCompositionRootFactoryOverride(
            _ => Task.FromResult(CreateTestCompositionRoot(tokens, enterpriseExists, repository)));
        await InvokeHandlerAsync(handler, context);
        return await ReadResponseAsync(context);
    }

    private static DefaultHttpContext CreateApiContext(ILoggerFactory? loggerFactory = null)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.RequestServices = new SingleServiceProvider(
            typeof(ILoggerFactory),
            loggerFactory ?? NullLoggerFactory.Instance);
        return context;
    }

    private static async Task InvokeHandlerAsync(string handler, DefaultHttpContext context)
    {
        var api = new LandDemandApi();
        var contextProperty = typeof(LandDemandApi).BaseType?.GetProperty(
            "Context",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(contextProperty);
        contextProperty!.SetValue(api, context);

        var method = typeof(LandDemandApi).GetMethod(handler, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);
        await Assert.IsAssignableFrom<Task>(method!.Invoke(api, null));
    }

    private static async Task<ApiResult> ReadResponseAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return new ApiResult(context.Response.StatusCode, await reader.ReadToEndAsync());
    }

    private static void AssertResponse(ApiResult response, int statusCode, string error)
    {
        Assert.Equal(statusCode, response.StatusCode);
        Assert.Equal($"{{\"error\":\"{error}\"}}", response.Body);
    }

    private static IDisposable PushCompositionRootFactoryOverride(
        Func<CancellationToken, Task<EnterpriseCompositionRoot>> factory)
    {
        var method = typeof(LandDemandApi).GetMethod(
            "PushCompositionRootFactoryOverrideForTests",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (IDisposable)method!.Invoke(null, new object[] { factory })!;
    }

    private static EnterpriseCompositionRoot CreateTestCompositionRoot(
        IJwtTokenService tokens,
        bool enterpriseExists,
        ILandDemandRepository repository)
    {
        var enterpriseService = new EnterpriseService(
            new StubEnterpriseRepository(enterpriseExists));
        var landDemandService = new LandDemandService(
            enterpriseService,
            repository,
            () => DateTimeOffset.Parse("2026-08-06T00:00:00+08:00"));
        var authService = new AuthService(
            new StubUserRepository(),
            new StubPasswordHasher(),
            tokens,
            TimeSpan.FromHours(1),
            TimeSpan.FromDays(7));
        var constructor = typeof(EnterpriseCompositionRoot)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single();

        return (EnterpriseCompositionRoot)constructor.Invoke(new object[]
        {
            authService,
            enterpriseService,
            landDemandService,
            tokens,
            new Func<SqlSugarClient>(() => throw new NotSupportedException("Tests do not create SqlSugar clients."))
        });
    }

    private static JwtTokenService TestJwtTokenService() =>
        new(AuthOptions.From(new Dictionary<string, string?>
        {
            ["FGC_JWT_SIGNING_KEY"] = "test-signing-key-that-is-at-least-32-chars",
            ["FGC_JWT_ISSUER"] = "synthetic-issuer",
            ["FGC_JWT_LIFETIME_MINUTES"] = "60",
            ["FGC_JWT_REFRESH_LIFETIME_MINUTES"] = "10080"
        }));

    private static LandDemandRecord SyntheticRecord() =>
        new()
        {
            Id = 42,
            Businessname = "Synthetic Enterprise",
            Creditcode = "91330200SYNTHETIC",
            County = "Synthetic County",
            Region = "330212",
            Area = "330212",
            BuildingArea = 1234.56m,
            ExpectPark = "Synthetic Park",
            ExpectTime = "2026-12",
            IsDeploy = "1",
            DeployPark = "Park A,Park B",
            IsSpecialuse = "1",
            DeployLandtype = "Industrial",
            DeployHeight = 12.34m,
            DeployWeight = 56.78m,
            Investment = 1000m,
            ProjectHydm = "C3990",
            Keyindustry = "Synthetic Track",
            Futureindustry = "Synthetic Direction",
            PredYs = 100m,
            PredTax = 20m,
            PredRdex = 10m,
            PredUnitenergy = 5m,
            Projectdata = "Synthetic project",
            IsFinancing = "0",
            Contact = "Synthetic Contact",
            Office = "Synthetic Office",
            Phone = "13800000000",
            Landusedemand = "2",
            Updatetime = "2026-08-06 12:34:56",
            Updateuser = "must-not-leak"
        };

    private static Assembly? ResolveForguncyServerApi(object? sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name);
        return name.Name == "GrapeCity.Forguncy.ServerApi"
            ? Assembly.LoadFrom("D:\\Program Files\\Forguncy 8.0.4\\Website\\bin\\GrapeCity.Forguncy.ServerApi.dll")
            : null;
    }

    private sealed record ApiResult(int StatusCode, string Body);

    private sealed class StubEnterpriseRepository : IEnterpriseRepository
    {
        private readonly bool exists;

        public StubEnterpriseRepository(bool exists)
        {
            this.exists = exists;
        }

        public Task<EnterpriseProfile?> FindByCreditCodeAsync(
            string creditCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                exists
                    ? new EnterpriseProfile
                    {
                        UserId = 7,
                        CreditCode = creditCode,
                        BusinessName = "Synthetic Enterprise",
                        CountyName = "Synthetic County",
                        Region = "330212"
                    }
                    : null);
    }

    private sealed class StubLandDemandRepository : ILandDemandRepository
    {
        public LandDemandRecord? Existing { get; set; }

        public Exception? FindException { get; init; }

        public Task<LandDemandRecord?> FindByCreditCodeAsync(
            string creditCode,
            CancellationToken cancellationToken)
        {
            if (FindException is not null)
            {
                throw FindException;
            }

            return Task.FromResult(Existing);
        }

        public Task<LandDemandRecord> InsertAsync(
            LandDemandRecord record,
            CancellationToken cancellationToken)
        {
            Existing = record;
            return Task.FromResult(record);
        }

        public Task<bool> UpdateWritableFieldsAsync(
            string creditCode,
            LandDemandWriteRequest request,
            string updateTime,
            string updateUser,
            CancellationToken cancellationToken) => Task.FromResult(Existing is not null);
    }

    private sealed class StubUserRepository : IUserRepository
    {
        public Task<AuthUser?> FindByUsernameAsync(string creditCode, CancellationToken cancellationToken) =>
            Task.FromResult<AuthUser?>(null);
    }

    private sealed class StubPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => throw new NotSupportedException();

        public bool Verify(string password, string encodedHash) => false;
    }

    private sealed class SingleServiceProvider : IServiceProvider
    {
        private readonly Type serviceType;
        private readonly object service;

        public SingleServiceProvider(Type serviceType, object service)
        {
            this.serviceType = serviceType;
            this.service = service;
        }

        public object? GetService(Type requestedType) => requestedType == serviceType ? service : null;
    }

    private sealed class CapturingLoggerFactory : ILoggerFactory
    {
        public CapturingLogger Logger { get; } = new();

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName) => Logger;

        public void Dispose()
        {
        }
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) => NoopDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var fields = state as IEnumerable<KeyValuePair<string, object?>>
                ?? Array.Empty<KeyValuePair<string, object?>>();
            Entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), fields.ToArray(), exception));
        }
    }

    private sealed record LogEntry(
        LogLevel LogLevel,
        EventId EventId,
        string Message,
        IReadOnlyList<KeyValuePair<string, object?>> State,
        Exception? Exception);

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }

    private sealed class ThrowingReadStream : Stream
    {
        private readonly Exception exception;

        public ThrowingReadStream(Exception exception)
        {
            this.exception = exception;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw exception;
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Task.FromException<int>(exception);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
