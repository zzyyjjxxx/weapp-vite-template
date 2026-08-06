using System.Reflection;
using Newtonsoft.Json;
using ForguncyServerApi.Application;
using ForguncyServerApi.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ForguncyServerApi.Tests.Api;

public sealed class AuthApiSurfaceTests
{
    [Fact]
    public void AuthApi_reflection_surface_does_not_reference_logging_types()
    {
        WithAuthApiType(type =>
        {
            var referencedTypes = type
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Select(field => field.FieldType)
                .Concat(type.GetMethods(
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.Static |
                        BindingFlags.DeclaredOnly)
                    .SelectMany(method => new[] { method.ReturnType }.Concat(
                        method.GetParameters().Select(parameter => parameter.ParameterType))))
                .Select(referencedType => referencedType.FullName)
                .Where(fullName => fullName is not null)
                .ToArray();

            Assert.DoesNotContain(
                referencedTypes,
                fullName => fullName!.StartsWith("Microsoft.Extensions.Logging", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void Api_assembly_exposes_the_public_application_types()
    {
        WithAuthApiType(type =>
        {
            var exportedTypes = type.Assembly
                .GetExportedTypes()
                .Select(exportedType => exportedType.FullName)
                .Where(fullName => fullName is not null)
                .OrderBy(fullName => fullName)
                .ToArray();

            Assert.Equal(
                new[]
                {
                    "ForguncyServerApi.Api.AuthApi",
                    "ForguncyServerApi.Api.AuthCompositionRoot",
                    "ForguncyServerApi.Api.AuthDiagnostics",
                    "ForguncyServerApi.Api.LoginRequestFormatException",
                    "ForguncyServerApi.Api.LoginRequestReader",
                    "ForguncyServerApi.Api.RetryableAsyncCache`1",
                    "ForguncyServerApi.Application.AuthService",
                    "ForguncyServerApi.Application.LoginRequest",
                    "ForguncyServerApi.Application.LoginResult",
                    "ForguncyServerApi.Application.LoginStatus",
                    "ForguncyServerApi.Application.RefreshResult",
                    "ForguncyServerApi.Application.RefreshStatus",
                    "ForguncyServerApi.Application.TokenPair",
                    "ForguncyServerApi.Configuration.AuthOptions",
                    "ForguncyServerApi.Domain.AuthUser",
                    "ForguncyServerApi.Infrastructure.AuthSqlSugarClientFactory",
                    "ForguncyServerApi.Infrastructure.ForguncyConfigConnectionStringReader",
                    "ForguncyServerApi.Infrastructure.ForguncyJwtConfigurationReader",
                    "ForguncyServerApi.Infrastructure.IUserRepository",
                    "ForguncyServerApi.Infrastructure.UserRepository",
                    "ForguncyServerApi.Security.IJwtTokenService",
                    "ForguncyServerApi.Security.IPasswordHasher",
                    "ForguncyServerApi.Security.JwtTokenService",
                    "ForguncyServerApi.Security.PasswordHasher",
                    "System.Runtime.CompilerServices.IsExternalInit"
                },
                exportedTypes);
        });
    }

    [Fact]
    public void AuthApi_exposes_only_parameterless_login_and_refresh_post_methods()
    {
        WithAuthApiType(type =>
        {
            var declaredPublicMethods = type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Assert.Equal(2, declaredPublicMethods.Length);

            AssertPublicPostMethod(declaredPublicMethods, "Login");
            AssertPublicPostMethod(declaredPublicMethods, "Refresh");

            Assert.Equal("GrapeCity.Forguncy.ServerApi.ForguncyApi", type.BaseType?.FullName);
            Assert.DoesNotContain(type.GetMethods(), method => method.Name is "Issue" or "Validate");
        });
    }

    [Fact]
    public void AuthCompositionRoot_requires_forguncy_data_access_before_initialization()
    {
        WithAuthApiType(type =>
        {
            var compositionRoot = type.Assembly.GetType("ForguncyServerApi.Api.AuthCompositionRoot");
            Assert.NotNull(compositionRoot);

            var createAsync = compositionRoot!.GetMethod(
                "CreateAsync",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(createAsync);

            var parameters = createAsync!.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal("GrapeCity.Forguncy.ServerApi.IDataAccess", parameters[0].ParameterType.FullName);
            Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        });
    }

    [Fact]
    public void AuthCompositionRoot_has_no_database_initializer_or_startup_schema_writes()
    {
        WithAuthApiType(type =>
        {
            Assert.Null(type.Assembly.GetType("ForguncyServerApi.Infrastructure.AuthDbInitializer"));

            var source = File.ReadAllText(SourceFile("Api", "AuthCompositionRoot.cs"));
            Assert.DoesNotContain("AuthDbInitializer", source);
            Assert.DoesNotContain("EnsureCreated", source);
        });
    }

    [Fact]
    public void AuthCompositionRoot_loads_jwt_options_from_the_forguncy_config_table()
    {
        var source = File.ReadAllText(SourceFile("Api", "AuthCompositionRoot.cs"));

        Assert.Contains("ForguncyJwtConfigurationReader.ReadOrCreate", source);
        Assert.DoesNotContain("AuthOptions.FromEnvironment", source);
    }

    [Fact]
    public void AuthCompositionRoot_initializes_from_config_rows_without_environment_variables()
    {
        WithAuthApiType(type =>
        {
            var compositionRoot = type.Assembly.GetType("ForguncyServerApi.Api.AuthCompositionRoot");
            Assert.NotNull(compositionRoot);

            var createAsync = compositionRoot!.GetMethod(
                "CreateAsync",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(createAsync);

            var fake = Infrastructure.ForguncyJwtConfigurationReaderTests.CapturingDataAccess.Create(
                new Dictionary<string, Dictionary<string, object>?>
                {
                    ["ssl"] = new Dictionary<string, object>
                    {
                        ["id"] = 1,
                        ["value"] = "Server=synthetic;Database=synthetic;"
                    },
                    ["FGC_JWT_SIGNING_KEY"] = new Dictionary<string, object>
                    {
                        ["id"] = 2,
                        ["value"] = new string('k', 32)
                    },
                    ["FGC_JWT_ISSUER"] = new Dictionary<string, object>
                    {
                        ["id"] = 3,
                        ["value"] = "existing-issuer"
                    },
                    ["FGC_JWT_EXPIRES_MINUTES"] = new Dictionary<string, object>
                    {
                        ["id"] = 4,
                        ["value"] = "15"
                    },
                    ["FGC_JWT_REFRESH_EXPIRES_MINUTES"] = new Dictionary<string, object>
                    {
                        ["id"] = 5,
                        ["value"] = "10080"
                    }
                });

            var task = Assert.IsAssignableFrom<Task>(createAsync!.Invoke(
                null,
                new[] { fake.DataAccess, CancellationToken.None }));
            task.GetAwaiter().GetResult();

            Assert.Equal(
                new[]
                {
                    "ssl",
                    "FGC_JWT_SIGNING_KEY",
                    "FGC_JWT_ISSUER",
                    "FGC_JWT_EXPIRES_MINUTES",
                    "FGC_JWT_REFRESH_EXPIRES_MINUTES"
                },
                fake.ReadItems);
            Assert.Empty(fake.Additions);
            Assert.Empty(fake.Updates);
        });
    }

    [Fact]
    public void Real_user_deployment_surface_has_no_legacy_bootstrap_assets_or_guidance()
    {
        var projectRoot = ProjectRoot();
        var productionFiles = Directory
            .EnumerateFiles(projectRoot, "*", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}"))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => Path.GetExtension(path) is ".cs" or ".md" or ".sql")
            .ToArray();
        var deploymentSurface = string.Join(
            Environment.NewLine,
            productionFiles.Select(File.ReadAllText));

        Assert.DoesNotContain("jwt_users", deploymentSurface);
        Assert.DoesNotContain("EnsureCreated", deploymentSurface);
        Assert.DoesNotContain("FGC_AUTH_BOOTSTRAP_", deploymentSurface);
        Assert.DoesNotContain("AuthDbInitializer", deploymentSurface);
        Assert.DoesNotContain(Path.Combine(projectRoot, "sql", "001-create-database.sql"), productionFiles);
    }

    [Fact]
    public void AuthApi_server_error_payload_is_fixed_and_contains_no_sensitive_detail()
    {
        WithAuthApiType(type =>
        {
            var factory = type.GetMethod("CreateServerErrorResponse", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(factory);

            var response = factory!.Invoke(null, null);
            Assert.NotNull(response);
            var json = JsonConvert.SerializeObject(response);

            Assert.Equal("{\"error\":\"server_error\"}", json);
            var lowerJson = json.ToLowerInvariant();
            Assert.DoesNotContain("exception", lowerJson);
            Assert.DoesNotContain("config", lowerJson);
            Assert.DoesNotContain("secret", lowerJson);
            Assert.DoesNotContain("password", lowerJson);
            Assert.DoesNotContain("connection", lowerJson);
        });
    }

    [Fact]
    public void AuthApi_unexpected_exception_records_only_a_sanitized_server_diagnostic()
    {
        WithAuthApiType(type =>
        {
            var recorder = type.GetMethod(
                "RecordUnexpectedLoginFailure",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(recorder);

            const string secret = "password=diagnostic-must-not-log-this";
            var loggerFactory = new CapturingLoggerFactory();
            var services = new SingleServiceProvider(typeof(ILoggerFactory), loggerFactory);

            try
            {
                throw new InvalidOperationException(secret);
            }
            catch (Exception exception)
            {
                recorder!.Invoke(null, new object?[] { services, exception });
            }

            var entry = Assert.Single(loggerFactory.Logger.Entries);
            Assert.Equal(LogLevel.Error, entry.LogLevel);
            Assert.Equal(1001, entry.EventId.Id);
            Assert.Equal("AuthLoginUnexpectedFailure", entry.EventId.Name);
            Assert.Null(entry.Exception);

            var fields = entry.State
                .Where(field => field.Key != "{OriginalFormat}")
                .ToDictionary(field => field.Key, field => field.Value?.ToString());
            Assert.Equal(2, fields.Count);
            Assert.Equal("auth.login.unexpected_failure", fields["OperationCode"]);
            Assert.Equal(nameof(InvalidOperationException), fields["ExceptionType"]);

            var diagnostic = string.Join(
                "|",
                entry.Message,
                string.Join("|", entry.State.Select(field => $"{field.Key}={field.Value}")));
            var lowerDiagnostic = diagnostic.ToLowerInvariant();
            Assert.DoesNotContain(secret, diagnostic);
            Assert.DoesNotContain("password", lowerDiagnostic);
            Assert.DoesNotContain("connection string", lowerDiagnostic);
            Assert.DoesNotContain("stack trace", lowerDiagnostic);
        });
    }

    [Fact]
    public void AuthApi_refresh_unexpected_exception_records_only_a_sanitized_server_diagnostic()
    {
        WithAuthApiType(type =>
        {
            var recorder = type.GetMethod(
                "RecordUnexpectedRefreshFailure",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(recorder);

            const string secret = "password=refresh-diagnostic-must-not-log-this";
            var loggerFactory = new CapturingLoggerFactory();
            var services = new SingleServiceProvider(typeof(ILoggerFactory), loggerFactory);

            try
            {
                throw new InvalidOperationException(secret);
            }
            catch (Exception exception)
            {
                recorder!.Invoke(null, new object?[] { services, exception });
            }

            var entry = Assert.Single(loggerFactory.Logger.Entries);
            Assert.Equal(LogLevel.Error, entry.LogLevel);
            Assert.Equal(1002, entry.EventId.Id);
            Assert.Equal("AuthRefreshUnexpectedFailure", entry.EventId.Name);
            Assert.Null(entry.Exception);

            var fields = entry.State
                .Where(field => field.Key != "{OriginalFormat}")
                .ToDictionary(field => field.Key, field => field.Value?.ToString());
            Assert.Equal(2, fields.Count);
            Assert.Equal("auth.refresh.unexpected_failure", fields["OperationCode"]);
            Assert.Equal(nameof(InvalidOperationException), fields["ExceptionType"]);

            var diagnostic = string.Join(
                "|",
                entry.Message,
                string.Join("|", entry.State.Select(field => $"{field.Key}={field.Value}")));
            var lowerDiagnostic = diagnostic.ToLowerInvariant();
            Assert.DoesNotContain(secret, diagnostic);
            Assert.DoesNotContain("password", lowerDiagnostic);
            Assert.DoesNotContain("connection string", lowerDiagnostic);
            Assert.DoesNotContain("stack trace", lowerDiagnostic);
        });
    }

    [Fact]
    public async Task AuthApi_request_read_failure_returns_fixed_server_error_and_records_a_diagnostic()
    {
        await WithAuthApiTypeAsync(async type =>
        {
            const string sensitiveDetail = "password=request-stream-detail-must-not-escape";
            var loggerFactory = new CapturingLoggerFactory();
            var context = new DefaultHttpContext();
            context.Request.ContentType = "application/json";
            context.Request.Body = new ThrowingReadStream(new IOException(sensitiveDetail));
            context.RequestServices = new SingleServiceProvider(typeof(ILoggerFactory), loggerFactory);
            context.Response.Body = new MemoryStream();

            var api = Activator.CreateInstance(type);
            Assert.NotNull(api);
            var contextProperty = type.BaseType?.GetProperty(
                "Context",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(contextProperty);
            contextProperty!.SetValue(api, context);

            var login = type.GetMethod("Login", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(login);
            var loginTask = Assert.IsAssignableFrom<Task>(login!.Invoke(api, null));

            await loginTask;

            context.Response.Body.Position = 0;
            using var responseReader = new StreamReader(context.Response.Body);
            var responseBody = await responseReader.ReadToEndAsync();

            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
            Assert.Equal("{\"error\":\"server_error\"}", responseBody);
            Assert.DoesNotContain(sensitiveDetail, responseBody);

            var entry = Assert.Single(loggerFactory.Logger.Entries);
            Assert.Equal(LogLevel.Error, entry.LogLevel);
            Assert.Equal(nameof(IOException), entry.State.Single(field => field.Key == "ExceptionType").Value);
            Assert.Null(entry.Exception);
            Assert.DoesNotContain(sensitiveDetail, entry.Message);
        });
    }

    [Fact]
    public void AuthApi_maps_login_outcomes_to_200_400_and_401_responses()
    {
        WithAuthApiType(type =>
        {
            var mapper = type.GetMethod("CreateLoginResponse", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(mapper);

            AssertMappedResponse(
                mapper!,
                new LoginResult(
                    LoginStatus.Success,
                    new TokenPair("signed-token", "refresh-token", 3600, 7200),
                    new AuthUser { Id = 3, Username = "demo" }),
                200,
                "{\"access_token\":\"signed-token\",\"refresh_token\":\"refresh-token\",\"token_type\":\"Bearer\",\"expires_in\":3600,\"refresh_expires_in\":7200}");
            AssertMappedResponse(
                mapper!,
                new LoginResult(LoginStatus.InvalidRequest, null, null),
                400,
                "{\"error\":\"invalid_request\"}");
            AssertMappedResponse(
                mapper!,
                new LoginResult(LoginStatus.InvalidCredentials, null, null),
                401,
                "{\"error\":\"invalid_credentials\"}");
        });
    }

    [Fact]
    public void AuthApi_maps_refresh_outcomes_to_200_400_and_401_responses()
    {
        WithAuthApiType(type =>
        {
            var mapper = type.GetMethod("CreateRefreshResponse", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(mapper);

            AssertMappedResponse(
                mapper!,
                new RefreshResult(
                    RefreshStatus.Success,
                    new TokenPair("new-access-token", "new-refresh-token", 1800, 5400)),
                200,
                "{\"access_token\":\"new-access-token\",\"refresh_token\":\"new-refresh-token\",\"token_type\":\"Bearer\",\"expires_in\":1800,\"refresh_expires_in\":5400}");
            AssertMappedResponse(
                mapper!,
                new RefreshResult(RefreshStatus.InvalidRequest, null),
                400,
                "{\"error\":\"invalid_request\"}");
            AssertMappedResponse(
                mapper!,
                new RefreshResult(RefreshStatus.InvalidToken, null),
                401,
                "{\"error\":\"invalid_refresh_token\"}");
        });
    }

    private static void AssertMappedResponse(
        MethodInfo mapper,
        object result,
        int expectedStatusCode,
        string expectedJson)
    {
        var mapped = mapper.Invoke(null, new object[] { result });
        Assert.NotNull(mapped);
        var mappedType = mapped!.GetType();
        var statusCode = mappedType.GetProperty("StatusCode")?.GetValue(mapped);
        var payload = mappedType.GetProperty("Payload")?.GetValue(mapped);

        Assert.Equal(expectedStatusCode, statusCode);
        Assert.NotNull(payload);
        Assert.Equal(expectedJson, JsonConvert.SerializeObject(payload));
    }

    private static void AssertPublicPostMethod(IEnumerable<MethodInfo> declaredPublicMethods, string expectedName)
    {
        var method = Assert.Single(declaredPublicMethods.Where(candidate => candidate.Name == expectedName));
        Assert.Equal(typeof(Task), method.ReturnType);
        Assert.Empty(method.GetParameters());

        var attributes = method.GetCustomAttributes(inherit: false).ToArray();
        Assert.Single(attributes.Where(attribute =>
            attribute.GetType().FullName == "GrapeCity.Forguncy.ServerApi.PostAttribute"));
        Assert.DoesNotContain(attributes, attribute =>
            attribute.GetType().FullName == "GrapeCity.Forguncy.ServerApi.GetAttribute");
    }

    private static void WithAuthApiType(Action<Type> action)
    {
        ResolveEventHandler handler = ResolveForguncyServerApi;
        AppDomain.CurrentDomain.AssemblyResolve += handler;
        try
        {
            var type = Assembly.Load("ForguncyServerApi").GetType("ForguncyServerApi.Api.AuthApi");
            Assert.NotNull(type);
            action(type!);
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= handler;
        }
    }

    private static async Task WithAuthApiTypeAsync(Func<Type, Task> action)
    {
        ResolveEventHandler handler = ResolveForguncyServerApi;
        AppDomain.CurrentDomain.AssemblyResolve += handler;
        try
        {
            var type = Assembly.Load("ForguncyServerApi").GetType("ForguncyServerApi.Api.AuthApi");
            Assert.NotNull(type);
            await action(type!);
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= handler;
        }
    }

    private static Assembly? ResolveForguncyServerApi(object? sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name);
        return name.Name == "GrapeCity.Forguncy.ServerApi"
            ? Assembly.LoadFrom("D:\\Program Files\\Forguncy 8.0.4\\Website\\bin\\GrapeCity.Forguncy.ServerApi.dll")
            : null;
    }

    private static string ProjectRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string SourceFile(params string[] segments) => Path.Combine(
        new[] { ProjectRoot() }.Concat(segments).ToArray());

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

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw exception;

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) => Task.FromException<int>(exception);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class LogEntry
    {
        public LogEntry(
            LogLevel logLevel,
            EventId eventId,
            string message,
            IReadOnlyList<KeyValuePair<string, object?>> state,
            Exception? exception)
        {
            LogLevel = logLevel;
            EventId = eventId;
            Message = message;
            State = state;
            Exception = exception;
        }

        public LogLevel LogLevel { get; }

        public EventId EventId { get; }

        public string Message { get; }

        public IReadOnlyList<KeyValuePair<string, object?>> State { get; }

        public Exception? Exception { get; }
    }
}
