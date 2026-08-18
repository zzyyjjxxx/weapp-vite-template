using System.Reflection;
using ForguncyServerApi.Application;
using ForguncyServerApi.Api;
using ForguncyServerApi.Configuration;
using ForguncyServerApi.Domain;
using ForguncyServerApi.Infrastructure;
using ForguncyServerApi.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SqlSugar;
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
    public void EnterpriseApi_exposes_only_parameterless_auth_and_verification_methods()
    {
        WithEnterpriseApiType(type =>
        {
            var declaredPublicMethods = type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Assert.Equal(5, declaredPublicMethods.Length);

            AssertPublicPostMethod(declaredPublicMethods, "Login");
            AssertPublicPostMethod(declaredPublicMethods, "Refresh");
            AssertPublicPostMethod(declaredPublicMethods, "SendCode");
            AssertPublicPostMethod(declaredPublicMethods, "VerifyCode");
            AssertPublicGetMethod(declaredPublicMethods, "GetInfo");

            Assert.Equal("GrapeCity.Forguncy.ServerApi.ForguncyApi", type.BaseType?.FullName);
            Assert.DoesNotContain(type.GetMethods(), method => method.Name is "Issue" or "Validate");
        });
    }

    [Fact]
    public void README_documents_exactly_the_eight_formal_routes_without_legacy_auth_aliases()
    {
        var readme = File.ReadAllText(SourceFile("README.md"));
        var documentedRoutes = System.Text.RegularExpressions.Regex
            .Matches(
                readme.Split(new[] { "## Authentication" }, StringSplitOptions.None)[0],
                "^(GET|POST) /customapi/[a-z]+/[a-z]+(?=\\r?$)",
                System.Text.RegularExpressions.RegexOptions.Multiline)
            .Cast<System.Text.RegularExpressions.Match>()
            .Select(match => match.Value)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "POST /customapi/enterpriseapi/login",
                "POST /customapi/enterpriseapi/refresh",
                "GET /customapi/enterpriseapi/getinfo",
                "POST /customapi/enterpriseapi/sendcode",
                "POST /customapi/enterpriseapi/verifycode",
                "GET /customapi/landdemandapi/getlanddemand",
                "POST /customapi/landdemandapi/addlanddemand",
                "POST /customapi/landdemandapi/updatelanddemand"
            },
            documentedRoutes);
        Assert.DoesNotContain("/customapi/authapi/login", readme);
        Assert.DoesNotContain("/customapi/authapi/refresh", readme);
        Assert.DoesNotContain("AuthApi", readme);
    }

    [Fact]
    public void README_documents_identity_configuration_and_fixed_error_contracts_without_credentials()
    {
        var readme = File.ReadAllText(SourceFile("README.md"));

        Assert.Contains("config.item='ssl'", readme);
        Assert.Contains("c_userinfo", readme);
        Assert.Contains("m_preliminary_list.county", readme);
        Assert.Contains("yj_regioninfo.id", readme);
        Assert.Contains("JWT `name` claim", readme);
        Assert.Contains("refresh token", readme);
        Assert.Contains("| Login | 400 | `{\"error\":\"invalid_request\"}` |", readme);
        Assert.Contains("| Login | 401 | `{\"error\":\"invalid_credentials\"}` |", readme);
        Assert.Contains("| Refresh | 401 | `{\"error\":\"invalid_refresh_token\"}` |", readme);
        Assert.Contains("| Business operations | 401 | `{\"error\":\"invalid_token\"}` |", readme);
        Assert.Contains("| Add land demand | 409 | `{\"error\":\"land_demand_exists\"}` |", readme);
        Assert.Contains("| All routes | 500 | `{\"error\":\"server_error\"}` |", readme);

        Assert.DoesNotMatch(
            "(?is)(server|host|data source)\\s*=.*?(password|pwd)\\s*=",
            readme);
        Assert.DoesNotMatch("(?i)mysql://[^\\s]+:[^\\s]+@", readme);
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
            Assert.Equal(
                typeof(SmsVerificationService),
                compositionRoot.GetMethod("CreateSmsVerificationService")!.ReturnType);
        });
    }

    [Fact]
    public void EnterpriseApi_uses_enterprise_composition_root_and_response_writer_without_private_field_reflection()
    {
        var source = File.ReadAllText(SourceFile("Api", "EnterpriseApi.cs"));

        Assert.Contains("EnterpriseCompositionRoot", source);
        Assert.Contains("EnterpriseCompositionRoot.GetOrCreateAsync", source);
        Assert.Contains("ApiResponseWriter.WriteJsonAsync", source);
        Assert.DoesNotContain("RetryableAsyncCache<EnterpriseCompositionRoot>", source);
        Assert.DoesNotContain("GetField(", source);
        Assert.DoesNotContain("BindingFlags.NonPublic", source);
    }

    [Fact]
    public void EnterpriseCompositionRoot_exposes_shared_cached_get_or_create_facility_for_future_api_reuse()
    {
        WithEnterpriseApiType(type =>
        {
            var compositionRoot = type.Assembly.GetType("ForguncyServerApi.Api.EnterpriseCompositionRoot");
            Assert.NotNull(compositionRoot);

            var getOrCreateAsync = compositionRoot!.GetMethod(
                "GetOrCreateAsync",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(getOrCreateAsync);

            var parameters = getOrCreateAsync!.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal("GrapeCity.Forguncy.ServerApi.IDataAccess", parameters[0].ParameterType.FullName);
            Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
            Assert.Equal(typeof(Task<>).MakeGenericType(compositionRoot), getOrCreateAsync.ReturnType);
        });
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

            Assert.Equal("{\"error\":\"invalid_token\"}", JsonConvert.SerializeObject(payload));
        });
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("Basic synthetic", false)]
    [InlineData("Bearer {0}", true)]
    [InlineData("Bearer malformed token", false)]
    public async Task EnterpriseApi_get_info_handler_maps_missing_malformed_and_refresh_use_tokens_to_401_invalid_token(
        string? authorizationHeader,
        bool useRefreshToken)
    {
        await WithEnterpriseApiTypeAsync(async type =>
        {
            var tokens = TestJwtTokenService();
            var profileLookupCount = 0;
            using var factoryOverride = PushCompositionRootFactoryOverride(
                type,
                _ => Task.FromResult(CreateTestCompositionRoot(
                    tokens,
                    (_, _) =>
                    {
                        profileLookupCount++;
                        return Task.FromResult<EnterpriseProfile?>(null);
                    })));

            var context = CreateApiContext();
            if (authorizationHeader is not null)
            {
                var token = useRefreshToken
                    ? tokens.CreateRefreshToken(new AuthUser { Id = 8, Username = "91330200REFRESH" })
                    : "unused";
                context.Request.Headers["Authorization"] = string.Format(authorizationHeader, token);
            }

            await InvokeGetInfoAsync(type, context);

            await AssertJsonResponseAsync(context, 401, "{\"error\":\"invalid_token\"}");
            Assert.Equal(0, profileLookupCount);
        });
    }

    [Fact]
    public async Task EnterpriseApi_get_info_handler_returns_profile_not_found_for_missing_enterprise()
    {
        await WithEnterpriseApiTypeAsync(async type =>
        {
            var tokens = TestJwtTokenService();
            EnterpriseIdentity? capturedIdentity = null;
            using var factoryOverride = PushCompositionRootFactoryOverride(
                type,
                cancellationToken => Task.FromResult(CreateTestCompositionRoot(
                    tokens,
                    (identity, ct) =>
                    {
                        capturedIdentity = identity;
                        return Task.FromResult<EnterpriseProfile?>(null);
                    })));

            var context = CreateApiContext();
            context.Request.Headers["Authorization"] = $"Bearer {tokens.CreateToken(new AuthUser { Id = 9, Username = "91330200MISSING" })}";

            await InvokeGetInfoAsync(type, context);

            await AssertJsonResponseAsync(context, 404, "{\"error\":\"enterprise_not_found\"}");
            Assert.NotNull(capturedIdentity);
            Assert.Equal("91330200MISSING", capturedIdentity.CreditCode);
        });
    }

    [Fact]
    public async Task EnterpriseApi_get_info_handler_returns_businessname_creditcode_county_and_region_for_valid_access_token()
    {
        await WithEnterpriseApiTypeAsync(async type =>
        {
            var tokens = TestJwtTokenService();
            using var factoryOverride = PushCompositionRootFactoryOverride(
                type,
                _ => Task.FromResult(CreateTestCompositionRoot(
                    tokens,
                    (identity, _) => Task.FromResult<EnterpriseProfile?>(new EnterpriseProfile
                    {
                        UserId = identity.UserId,
                        BusinessName = "Synthetic Enterprise",
                        CreditCode = identity.CreditCode,
                        CountyName = "Yinzhou",
                        Region = "330212",
                        Phone = "13800000000"
                    }))));

            var context = CreateApiContext();
            context.Request.Headers["Authorization"] = $"Bearer {tokens.CreateToken(new AuthUser { Id = 10, Username = "91330200SUCCESS" })}";

            await InvokeGetInfoAsync(type, context);

            await AssertJsonResponseAsync(
                context,
                200,
                "{\"businessname\":\"Synthetic Enterprise\",\"creditcode\":\"91330200SUCCESS\",\"county\":\"Yinzhou\",\"region\":\"330212\",\"phone\":\"13800000000\"}");
        });
    }

    [Fact]
    public async Task EnterpriseApi_get_info_handler_returns_fixed_server_error_and_sanitized_diagnostic_for_unexpected_exception()
    {
        await WithEnterpriseApiTypeAsync(async type =>
        {
            const string secret = "password=get-info-secret-should-not-escape";
            var tokens = TestJwtTokenService();
            var loggerFactory = new CapturingLoggerFactory();
            using var factoryOverride = PushCompositionRootFactoryOverride(
                type,
                _ => Task.FromResult(CreateTestCompositionRoot(
                    tokens,
                    (_, _) => throw new InvalidOperationException(secret))));

            var context = CreateApiContext(loggerFactory);
            context.Request.Headers["Authorization"] = $"Bearer {tokens.CreateToken(new AuthUser { Id = 11, Username = "91330200ERROR" })}";

            await InvokeGetInfoAsync(type, context);

            await AssertJsonResponseAsync(context, 500, "{\"error\":\"server_error\"}");

            var entry = Assert.Single(loggerFactory.Logger.Entries);
            Assert.Equal(LogLevel.Error, entry.LogLevel);
            Assert.Equal("EnterpriseGetInfoUnexpectedFailure", entry.EventId.Name);
            AssertDiagnosticFields(entry, "enterprise.get_info");
            AssertNoSecretInDiagnostic(entry, secret);
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
    public void Api_error_diagnostics_are_opt_in_and_never_return_exception_messages_by_default()
    {
        var diagnosticsType = typeof(EnterpriseApi).Assembly.GetType(
            "ForguncyServerApi.Api.ApiErrorDiagnostics");
        Assert.NotNull(diagnosticsType);

        var factory = diagnosticsType!.GetMethod(
            "CreateServerError",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(factory);

        const string secret = "password=diagnostics-secret;server=private";
        var exception = new InvalidOperationException(secret);
        var context = new DefaultHttpContext();

        var fixedPayload = factory!.Invoke(
            null,
            new object?[] { context.Request, "enterprise.login", exception });
        Assert.Equal("{\"error\":\"server_error\"}", JsonConvert.SerializeObject(fixedPayload));

        context.Request.QueryString = new QueryString("?diagnostics=1");
        var diagnosticPayload = factory.Invoke(
            null,
            new object?[] { context.Request, "enterprise.login", exception });
        var diagnosticJson = JsonConvert.SerializeObject(diagnosticPayload);
        var diagnosticObject = JObject.Parse(diagnosticJson);

        Assert.Equal("server_error", diagnosticObject["error"]?.Value<string>());
        Assert.Equal("enterprise.login", diagnosticObject["operation"]?.Value<string>());
        Assert.Equal(nameof(InvalidOperationException), diagnosticObject["exception_type"]?.Value<string>());
        Assert.Equal("unexpected_exception", diagnosticObject["detail_code"]?.Value<string>());
        Assert.Null(diagnosticObject["message"]);
        Assert.DoesNotContain(secret, diagnosticJson);
        Assert.DoesNotContain("password", diagnosticJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("server=private", diagnosticJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Api_error_diagnostics_include_only_allowlisted_messages()
    {
        var diagnosticsType = typeof(EnterpriseApi).Assembly.GetType(
            "ForguncyServerApi.Api.ApiErrorDiagnostics");
        Assert.NotNull(diagnosticsType);

        var factory = diagnosticsType!.GetMethod(
            "CreateServerError",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(factory);

        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?diagnostics=true");
        var payload = factory!.Invoke(
            null,
            new object?[]
            {
                context.Request,
                "enterprise.send_code",
                new InvalidOperationException("The Forguncy SMS configuration is invalid.")
            });
        var json = JsonConvert.SerializeObject(payload);
        var response = JObject.Parse(json);

        Assert.Equal("sms_config_invalid", response["detail_code"]?.Value<string>());
        Assert.Equal(
            "The Forguncy SMS configuration is invalid.",
            response["message"]?.Value<string>());
    }

    [Fact]
    public void EnterpriseApi_maps_send_code_outcomes_to_success_cooldown_and_failure_responses()
    {
        WithEnterpriseApiType(type =>
        {
            var mapper = type.GetMethod("CreateSendCodeResponse", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(mapper);

            var expiresAt = new DateTimeOffset(2026, 8, 7, 12, 39, 48, TimeSpan.FromHours(8));
            var retryAt = new DateTimeOffset(2026, 8, 7, 12, 35, 48, TimeSpan.FromHours(8));
            var expiresJson = JsonConvert.SerializeObject(expiresAt);
            var retryJson = JsonConvert.SerializeObject(retryAt);

            AssertMappedResponse(
                mapper!,
                new SendVerificationCodeResult(SendVerificationCodeStatus.Success, expiresAt, retryAt),
                200,
                $"{{\"success\":true,\"status\":\"success\",\"expires_at\":{expiresJson},\"retry_at\":{retryJson}}}");
            AssertMappedResponse(
                mapper!,
                new SendVerificationCodeResult(SendVerificationCodeStatus.Cooldown, expiresAt, retryAt),
                429,
                $"{{\"success\":false,\"status\":\"cooldown\",\"expires_at\":{expiresJson},\"retry_at\":{retryJson}}}");
            AssertMappedResponse(
                mapper!,
                new SendVerificationCodeResult(SendVerificationCodeStatus.Failed, expiresAt, retryAt),
                502,
                $"{{\"success\":false,\"status\":\"failed\",\"expires_at\":{expiresJson},\"retry_at\":{retryJson}}}");
        });
    }

    [Fact]
    public void EnterpriseApi_maps_verify_code_outcomes_to_success_failed_and_expired_responses()
    {
        WithEnterpriseApiType(type =>
        {
            var mapper = type.GetMethod("CreateVerifyCodeResponse", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(mapper);

            AssertMappedResponse(
                mapper!,
                new VerifyVerificationCodeResult(VerifyVerificationCodeStatus.Success),
                200,
                "{\"success\":true,\"status\":\"success\"}");
            AssertMappedResponse(
                mapper!,
                new VerifyVerificationCodeResult(VerifyVerificationCodeStatus.Failed),
                400,
                "{\"success\":false,\"status\":\"failed\"}");
            AssertMappedResponse(
                mapper!,
                new VerifyVerificationCodeResult(VerifyVerificationCodeStatus.Expired),
                410,
                "{\"success\":false,\"status\":\"expired\"}");
        });
    }

    [Fact]
    public void EnterpriseApi_maps_get_info_success_to_businessname_creditcode_county_and_region()
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
                    Region = "330212",
                    Phone = "13800000000"
                }
            });

            Assert.NotNull(mapped);
            var mappedType = mapped!.GetType();
            Assert.Equal(200, mappedType.GetProperty("StatusCode")!.GetValue(mapped));
            var payload = mappedType.GetProperty("Payload")!.GetValue(mapped);
            var json = JsonConvert.SerializeObject(payload);

            Assert.Equal(
                "{\"businessname\":\"Synthetic Enterprise\",\"creditcode\":\"91330200SYNTHETIC\",\"county\":\"Yinzhou\",\"region\":\"330212\",\"phone\":\"13800000000\"}",
                json);

            var propertyNames = JObject.Parse(json).Properties().Select(property => property.Name).ToArray();
            Assert.Equal(new[] { "businessname", "creditcode", "county", "region", "phone" }, propertyNames);
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

    private static DefaultHttpContext CreateApiContext(ILoggerFactory? loggerFactory = null)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.RequestServices = loggerFactory is null
            ? new SingleServiceProvider(typeof(ILoggerFactory), NullLoggerFactory.Instance)
            : new SingleServiceProvider(typeof(ILoggerFactory), loggerFactory);
        return context;
    }

    private static async Task InvokeGetInfoAsync(Type type, DefaultHttpContext context)
    {
        var api = Activator.CreateInstance(type);
        Assert.NotNull(api);
        var contextProperty = type.BaseType?.GetProperty(
            "Context",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(contextProperty);
        contextProperty!.SetValue(api, context);

        var getInfo = type.GetMethod("GetInfo", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(getInfo);
        var task = Assert.IsAssignableFrom<Task>(getInfo!.Invoke(api, null));
        await task;
    }

    private static async Task AssertJsonResponseAsync(
        DefaultHttpContext context,
        int expectedStatusCode,
        string expectedJson)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        Assert.Equal(expectedStatusCode, context.Response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
        Assert.Equal("no-store", context.Response.Headers["Cache-Control"].ToString());
        Assert.Equal("no-cache", context.Response.Headers["Pragma"].ToString());
        Assert.Equal(expectedJson, responseBody);
    }

    private static IDisposable PushCompositionRootFactoryOverride(
        Type enterpriseApiType,
        Func<CancellationToken, Task<EnterpriseCompositionRoot>> factory)
    {
        var pushOverride = enterpriseApiType.GetMethod(
            "PushCompositionRootFactoryOverrideForTests",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(pushOverride);

        return (IDisposable)pushOverride!.Invoke(null, new object[] { factory })!;
    }

    private static EnterpriseCompositionRoot CreateTestCompositionRoot(
        IJwtTokenService tokens,
        Func<EnterpriseIdentity, CancellationToken, Task<EnterpriseProfile?>> getProfileAsync)
    {
        var enterpriseService = new EnterpriseService(new DelegateEnterpriseRepository(getProfileAsync));
        var authService = new AuthService(
            new StubUserRepository(),
            new StubPasswordHasher(),
            tokens,
            TimeSpan.FromHours(1),
            TimeSpan.FromDays(7));
        var landDemandService = new LandDemandService(
            enterpriseService,
            new StubLandDemandRepository(),
            () => DateTimeOffset.Parse("2026-08-06T00:00:00+08:00"));

        var compositionRootType = Assembly.Load("ForguncyServerApi")
            .GetType("ForguncyServerApi.Api.EnterpriseCompositionRoot", throwOnError: true)!;
        var constructor = compositionRootType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single();

        return (EnterpriseCompositionRoot)constructor.Invoke(new object[]
        {
            authService,
            enterpriseService,
            landDemandService,
            tokens,
            new Func<SqlSugarClient>(() => throw new NotSupportedException("Test composition root does not create SqlSugar clients.")),
            null!
        });
    }

    private static JwtTokenService TestJwtTokenService() => new(TestOptions());

    private static AuthOptions TestOptions() =>
        AuthOptions.From(new Dictionary<string, string?>
        {
            ["FGC_JWT_SIGNING_KEY"] = "test-signing-key-that-is-at-least-32-chars",
            ["FGC_JWT_ISSUER"] = "synthetic-issuer",
            ["FGC_JWT_EXPIRES_MINUTES"] = "60",
            ["FGC_JWT_REFRESH_EXPIRES_MINUTES"] = "10080"
        });

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
            ? Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "GrapeCity.Forguncy.ServerApi.dll"))
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

    private sealed class DelegateEnterpriseRepository : IEnterpriseRepository
    {
        private readonly Func<EnterpriseIdentity, CancellationToken, Task<EnterpriseProfile?>> getProfileAsync;

        public DelegateEnterpriseRepository(Func<EnterpriseIdentity, CancellationToken, Task<EnterpriseProfile?>> getProfileAsync)
        {
            this.getProfileAsync = getProfileAsync;
        }

        public Task<EnterpriseProfile?> FindByCreditCodeAsync(string creditCode, CancellationToken cancellationToken)
        {
            var identity = new EnterpriseIdentity(1, creditCode);
            return getProfileAsync(identity, cancellationToken);
        }
    }

    private sealed class StubLandDemandRepository : ILandDemandRepository
    {
        public Task<LandDemandRecord?> FindByCreditCodeAsync(string creditCode, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<LandDemandRecord> InsertAsync(LandDemandRecord record, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> UpdateWritableFieldsAsync(
            string creditCode,
            LandDemandWriteRequest request,
            string updateTime,
            string updateUser,
            CancellationToken cancellationToken) => throw new NotSupportedException();
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
}
