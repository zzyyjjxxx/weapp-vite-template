using System.Reflection;
using ForguncyServerApi.Application;
using ForguncyServerApi.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ForguncyServerApi.Tests.Api;

public sealed class AuthApiSurfaceTests
{
    [Fact]
    public void EnterpriseApi_reflection_surface_does_not_reference_logging_types()
    {
        WithEnterpriseApiType(type =>
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
    public void Api_assembly_exposes_enterprise_surface_and_current_public_application_types()
    {
        WithEnterpriseApiType(type =>
        {
            var exportedTypes = type.Assembly
                .GetExportedTypes()
                .Select(exportedType => exportedType.FullName)
                .Where(fullName => fullName is not null)
                .OrderBy(fullName => fullName)
                .ToArray();

            Assert.Contains("ForguncyServerApi.Api.EnterpriseApi", exportedTypes);
            Assert.Contains("ForguncyServerApi.Api.EnterpriseCompositionRoot", exportedTypes);
            Assert.Contains("ForguncyServerApi.Api.EnterpriseDiagnostics", exportedTypes);
            Assert.Contains("ForguncyServerApi.Api.LoginRequestFormatException", exportedTypes);
            Assert.Contains("ForguncyServerApi.Api.LoginRequestReader", exportedTypes);
            Assert.Contains("ForguncyServerApi.Api.RetryableAsyncCache`1", exportedTypes);
            Assert.Contains("ForguncyServerApi.Application.AuthService", exportedTypes);
            Assert.Contains("ForguncyServerApi.Application.EnterpriseIdentity", exportedTypes);
            Assert.Contains("ForguncyServerApi.Application.EnterpriseService", exportedTypes);
            Assert.Contains("ForguncyServerApi.Application.LandDemandService", exportedTypes);
            Assert.Contains("ForguncyServerApi.Application.LandDemandResponse", exportedTypes);
            Assert.Contains("ForguncyServerApi.Application.LandDemandOperationResult", exportedTypes);
            Assert.Contains("ForguncyServerApi.Domain.EnterpriseProfile", exportedTypes);
            Assert.Contains("ForguncyServerApi.Infrastructure.EnterpriseRepository", exportedTypes);
            Assert.Contains("ForguncyServerApi.Infrastructure.IEnterpriseRepository", exportedTypes);
            Assert.Contains("ForguncyServerApi.Infrastructure.ILandDemandRepository", exportedTypes);
            Assert.Contains("ForguncyServerApi.Infrastructure.LandDemandRepository", exportedTypes);
            Assert.DoesNotContain("ForguncyServerApi.Api.AuthApi", exportedTypes);
            Assert.DoesNotContain("ForguncyServerApi.Api.AuthCompositionRoot", exportedTypes);
            Assert.DoesNotContain("ForguncyServerApi.Api.AuthDiagnostics", exportedTypes);
        });
    }

    [Fact]
    public void EnterpriseApi_exposes_only_parameterless_login_refresh_and_get_info_methods()
    {
        WithEnterpriseApiType(type =>
        {
            var declaredPublicMethods = type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Assert.Equal(3, declaredPublicMethods.Length);

            AssertPublicPostMethod(declaredPublicMethods, "Login");
            AssertPublicPostMethod(declaredPublicMethods, "Refresh");
            AssertPublicGetMethod(declaredPublicMethods, "GetInfo");

            Assert.Equal("GrapeCity.Forguncy.ServerApi.ForguncyApi", type.BaseType?.FullName);
            Assert.DoesNotContain(type.GetMethods(), method => method.Name is "Issue" or "Validate");
        });
    }

    [Fact]
    public void EnterpriseCompositionRoot_requires_forguncy_data_access_before_initialization()
    {
        WithEnterpriseApiType(type =>
        {
            var compositionRoot = type.Assembly.GetType("ForguncyServerApi.Api.EnterpriseCompositionRoot");
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
    public void EnterpriseCompositionRoot_exposes_auth_enterprise_land_demand_and_tokens()
    {
        WithEnterpriseApiType(type =>
        {
            var compositionRoot = type.Assembly.GetType("ForguncyServerApi.Api.EnterpriseCompositionRoot");
            Assert.NotNull(compositionRoot);

            Assert.Equal(typeof(Task<LoginResult>), compositionRoot!.GetMethod("LoginAsync")!.ReturnType);
            Assert.Equal(typeof(Task<RefreshResult>), compositionRoot.GetMethod("RefreshAsync")!.ReturnType);
            Assert.Equal(typeof(Task<EnterpriseProfile?>), compositionRoot.GetMethod("GetInfoAsync")!.ReturnType);
            Assert.Equal(typeof(AuthService), compositionRoot.GetProperty("AuthService")!.PropertyType);
            Assert.Equal(typeof(EnterpriseService), compositionRoot.GetProperty("EnterpriseService")!.PropertyType);
            Assert.Equal(typeof(LandDemandService), compositionRoot.GetProperty("LandDemandService")!.PropertyType);
            Assert.Equal("ForguncyServerApi.Security.IJwtTokenService", compositionRoot.GetProperty("Tokens")!.PropertyType.FullName);
        });
    }

    [Fact]
    public void EnterpriseApi_uses_enterprise_composition_root_and_response_writer_without_private_field_reflection()
    {
        var source = File.ReadAllText(SourceFile("Api", "EnterpriseApi.cs"));

        Assert.Contains("EnterpriseCompositionRoot", source);
        Assert.Contains("ApiResponseWriter.WriteJsonAsync", source);
        Assert.DoesNotContain("GetField(", source);
        Assert.DoesNotContain("BindingFlags.NonPublic", source);
    }

    [Fact]
    public void EnterpriseCompositionRoot_has_no_database_initializer_or_startup_schema_writes()
    {
        WithEnterpriseApiType(type =>
        {
            Assert.Null(type.Assembly.GetType("ForguncyServerApi.Infrastructure.AuthDbInitializer"));

            var source = File.ReadAllText(SourceFile("Api", "EnterpriseCompositionRoot.cs"));
            Assert.DoesNotContain("AuthDbInitializer", source);
            Assert.DoesNotContain("EnsureCreated", source);
        });
    }

    [Fact]
    public void EnterpriseCompositionRoot_loads_jwt_options_from_the_forguncy_config_table()
    {
        var source = File.ReadAllText(SourceFile("Api", "EnterpriseCompositionRoot.cs"));

        Assert.Contains("ForguncyJwtConfigurationReader.ReadOrCreate", source);
        Assert.DoesNotContain("AuthOptions.FromEnvironment", source);
    }

    [Fact]
    public void EnterpriseCompositionRoot_initializes_from_config_rows_without_environment_variables()
    {
        WithEnterpriseApiType(type =>
        {
            var compositionRoot = type.Assembly.GetType("ForguncyServerApi.Api.EnterpriseCompositionRoot");
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
    public void EnterpriseApi_server_error_payload_is_fixed_and_contains_no_sensitive_detail()
    {
        WithEnterpriseApiType(type =>
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
    public void EnterpriseApi_unexpected_exception_records_only_a_sanitized_login_diagnostic()
    {
        WithEnterpriseApiType(type =>
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
            Assert.Equal("EnterpriseLoginUnexpectedFailure", entry.EventId.Name);
            AssertDiagnosticFields(entry, "enterprise.login");
            AssertNoSecretInDiagnostic(entry, secret);
        });
    }

    [Fact]
    public void EnterpriseApi_unexpected_exception_records_only_a_sanitized_refresh_diagnostic()
    {
        WithEnterpriseApiType(type =>
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
            Assert.Equal("EnterpriseRefreshUnexpectedFailure", entry.EventId.Name);
            AssertDiagnosticFields(entry, "enterprise.refresh");
            AssertNoSecretInDiagnostic(entry, secret);
        });
    }

    [Fact]
    public void EnterpriseApi_unexpected_exception_records_only_a_sanitized_get_info_diagnostic()
    {
        WithEnterpriseApiType(type =>
        {
            var recorder = type.GetMethod(
                "RecordUnexpectedGetInfoFailure",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(recorder);

            const string secret = "password=get-info-diagnostic-must-not-log-this";
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
            Assert.Equal("EnterpriseGetInfoUnexpectedFailure", entry.EventId.Name);
            AssertDiagnosticFields(entry, "enterprise.get_info");
            AssertNoSecretInDiagnostic(entry, secret);
        });
    }

    [Fact]
    public async Task EnterpriseApi_request_read_failure_returns_fixed_server_error_and_records_a_diagnostic()
    {
        await WithEnterpriseApiTypeAsync(async type =>
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
            Assert.Equal("no-store", context.Response.Headers["Cache-Control"].ToString());
            Assert.Equal("no-cache", context.Response.Headers["Pragma"].ToString());
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
    public async Task EnterpriseApi_refresh_request_read_failure_sets_no_cache_headers_and_fixed_server_error()
    {
        await WithEnterpriseApiTypeAsync(async type =>
        {
            var loggerFactory = new CapturingLoggerFactory();
            var context = new DefaultHttpContext();
            context.Request.ContentType = "application/json";
            context.Request.Body = new ThrowingReadStream(new IOException("refresh-request-stream-failure"));
            context.RequestServices = new SingleServiceProvider(typeof(ILoggerFactory), loggerFactory);
            context.Response.Body = new MemoryStream();

            var api = Activator.CreateInstance(type);
            Assert.NotNull(api);
            var contextProperty = type.BaseType?.GetProperty(
                "Context",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(contextProperty);
            contextProperty!.SetValue(api, context);

            var refresh = type.GetMethod("Refresh", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(refresh);
            var refreshTask = Assert.IsAssignableFrom<Task>(refresh!.Invoke(api, null));

            await refreshTask;

            context.Response.Body.Position = 0;
            using var responseReader = new StreamReader(context.Response.Body);
            var responseBody = await responseReader.ReadToEndAsync();

            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
            Assert.Equal("no-store", context.Response.Headers["Cache-Control"].ToString());
            Assert.Equal("no-cache", context.Response.Headers["Pragma"].ToString());
            Assert.Equal("{\"error\":\"server_error\"}", responseBody);

            var entry = Assert.Single(loggerFactory.Logger.Entries);
            Assert.Equal(LogLevel.Error, entry.LogLevel);
            Assert.Equal(nameof(IOException), entry.State.Single(field => field.Key == "ExceptionType").Value);
            Assert.Null(entry.Exception);
        });
    }

    [Fact]
    public void EnterpriseApi_maps_get_info_invalid_access_token_to_401()
    {
        WithEnterpriseApiType(type =>
        {
            var mapper = type.GetMethod("CreateInvalidAccessTokenResponse", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(mapper);

            var mapped = mapper!.Invoke(null, null);
            Assert.NotNull(mapped);
            var mappedType = mapped!.GetType();
            Assert.Equal(401, mappedType.GetProperty("StatusCode")!.GetValue(mapped));
            var payload = mappedType.GetProperty("Payload")!.GetValue(mapped);

            Assert.Equal("{\"error\":\"invalid_access_token\"}", JsonConvert.SerializeObject(payload));
        });
    }

    [Fact]
    public void EnterpriseApi_maps_login_outcomes_to_200_400_and_401_responses()
    {
        WithEnterpriseApiType(type =>
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
    public void EnterpriseApi_maps_refresh_outcomes_to_200_400_and_401_responses()
    {
        WithEnterpriseApiType(type =>
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

    [Fact]
    public void EnterpriseApi_maps_get_info_success_to_only_businessname_creditcode_and_county()
    {
        WithEnterpriseApiType(type =>
        {
            var mapper = type.GetMethod("CreateGetInfoResponse", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(mapper);

            var mapped = mapper!.Invoke(null, new object[]
            {
                new EnterpriseProfile
                {
                    UserId = 9,
                    BusinessName = "Synthetic Enterprise",
                    CreditCode = "91330200SYNTHETIC",
                    CountyName = "Yinzhou",
                    Region = "330212"
                }
            });

            Assert.NotNull(mapped);
            var mappedType = mapped!.GetType();
            Assert.Equal(200, mappedType.GetProperty("StatusCode")!.GetValue(mapped));
            var payload = mappedType.GetProperty("Payload")!.GetValue(mapped);
            var json = JsonConvert.SerializeObject(payload);

            Assert.Equal(
                "{\"businessname\":\"Synthetic Enterprise\",\"creditcode\":\"91330200SYNTHETIC\",\"county\":\"Yinzhou\"}",
                json);

            var propertyNames = JObject.Parse(json).Properties().Select(property => property.Name).ToArray();
            Assert.Equal(new[] { "businessname", "creditcode", "county" }, propertyNames);
            Assert.DoesNotContain("region", propertyNames);
            Assert.DoesNotContain("id", propertyNames);
            Assert.DoesNotContain("updateuser", propertyNames);
            Assert.DoesNotContain("reviewstatus", propertyNames);
        });
    }

    [Fact]
    public void EnterpriseApi_maps_get_info_missing_profile_to_404()
    {
        WithEnterpriseApiType(type =>
        {
            var mapper = type.GetMethod("CreateGetInfoNotFoundResponse", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(mapper);

            var mapped = mapper!.Invoke(null, null);
            Assert.NotNull(mapped);
            var mappedType = mapped!.GetType();
            Assert.Equal(404, mappedType.GetProperty("StatusCode")!.GetValue(mapped));
            var payload = mappedType.GetProperty("Payload")!.GetValue(mapped);

            Assert.Equal("{\"error\":\"enterprise_not_found\"}", JsonConvert.SerializeObject(payload));
        });
    }

    [Fact]
    public void Real_user_deployment_surface_has_no_legacy_auth_aliases_or_bootstrap_assets()
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

        Assert.DoesNotContain("POST /customapi/authapi/login", deploymentSurface);
        Assert.DoesNotContain("POST /customapi/authapi/refresh", deploymentSurface);
        Assert.DoesNotContain("jwt_users", deploymentSurface);
        Assert.DoesNotContain("EnsureCreated", deploymentSurface);
        Assert.DoesNotContain("FGC_AUTH_BOOTSTRAP_", deploymentSurface);
        Assert.DoesNotContain("AuthDbInitializer", deploymentSurface);
        Assert.DoesNotContain(Path.Combine(projectRoot, "sql", "001-create-database.sql"), productionFiles);
    }

    private static void AssertMappedResponse(
        MethodInfo mapper,
        object result,
        int expectedStatusCode,
        string expectedJson)
    {
        var mapped = mapper.Invoke(null, new[] { result });
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

    private static void AssertPublicGetMethod(IEnumerable<MethodInfo> declaredPublicMethods, string expectedName)
    {
        var method = Assert.Single(declaredPublicMethods.Where(candidate => candidate.Name == expectedName));
        Assert.Equal(typeof(Task), method.ReturnType);
        Assert.Empty(method.GetParameters());

        var attributes = method.GetCustomAttributes(inherit: false).ToArray();
        Assert.Single(attributes.Where(attribute =>
            attribute.GetType().FullName == "GrapeCity.Forguncy.ServerApi.GetAttribute"));
        Assert.DoesNotContain(attributes, attribute =>
            attribute.GetType().FullName == "GrapeCity.Forguncy.ServerApi.PostAttribute");
    }

    private static void AssertDiagnosticFields(LogEntry entry, string expectedOperationCode)
    {
        Assert.Null(entry.Exception);
        var fields = entry.State
            .Where(field => field.Key != "{OriginalFormat}")
            .ToDictionary(field => field.Key, field => field.Value?.ToString());
        Assert.Equal(2, fields.Count);
        Assert.Equal(expectedOperationCode, fields["OperationCode"]);
        Assert.Equal(nameof(InvalidOperationException), fields["ExceptionType"]);
    }

    private static void AssertNoSecretInDiagnostic(LogEntry entry, string secret)
    {
        var diagnostic = string.Join(
            "|",
            entry.Message,
            string.Join("|", entry.State.Select(field => $"{field.Key}={field.Value}")));
        var lowerDiagnostic = diagnostic.ToLowerInvariant();
        Assert.DoesNotContain(secret, diagnostic);
        Assert.DoesNotContain("password", lowerDiagnostic);
        Assert.DoesNotContain("connection string", lowerDiagnostic);
        Assert.DoesNotContain("stack trace", lowerDiagnostic);
    }

    private static void WithEnterpriseApiType(Action<Type> action)
    {
        ResolveEventHandler handler = ResolveForguncyServerApi;
        AppDomain.CurrentDomain.AssemblyResolve += handler;
        try
        {
            var type = Assembly.Load("ForguncyServerApi").GetType("ForguncyServerApi.Api.EnterpriseApi");
            Assert.NotNull(type);
            action(type!);
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= handler;
        }
    }

    private static async Task WithEnterpriseApiTypeAsync(Func<Type, Task> action)
    {
        ResolveEventHandler handler = ResolveForguncyServerApi;
        AppDomain.CurrentDomain.AssemblyResolve += handler;
        try
        {
            var type = Assembly.Load("ForguncyServerApi").GetType("ForguncyServerApi.Api.EnterpriseApi");
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
