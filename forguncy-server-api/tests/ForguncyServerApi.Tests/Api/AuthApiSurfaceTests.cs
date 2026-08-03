using System.Reflection;
using System.Text.Json;
using ForguncyServerApi.Application;
using ForguncyServerApi.Domain;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ForguncyServerApi.Tests.Api;

public sealed class AuthApiSurfaceTests
{
    [Fact]
    public void AuthApi_exposes_only_the_login_post_method()
    {
        WithAuthApiType(type =>
        {
            var declaredPublicMethods = type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var login = Assert.Single(declaredPublicMethods);
            Assert.Equal("Login", login.Name);
            Assert.Equal(typeof(Task), login.ReturnType);
            Assert.Empty(login.GetParameters());

            var attributes = login.GetCustomAttributes(inherit: false).ToArray();
            Assert.Single(attributes.Where(attribute =>
                attribute.GetType().FullName == "GrapeCity.Forguncy.ServerApi.PostAttribute"));
            Assert.DoesNotContain(attributes, attribute =>
                attribute.GetType().FullName == "GrapeCity.Forguncy.ServerApi.GetAttribute");

            Assert.Equal("GrapeCity.Forguncy.ServerApi.ForguncyApi", type.BaseType?.FullName);
            Assert.DoesNotContain(type.GetMethods(), method => method.Name is "Issue" or "Validate");
        });
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
            var json = JsonSerializer.Serialize(response, response!.GetType());

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
                    "signed-token",
                    new AuthUser { Id = 3, Username = "demo" },
                    3600),
                200,
                "{\"access_token\":\"signed-token\",\"token_type\":\"Bearer\",\"expires_in\":3600,\"user\":{\"id\":3,\"username\":\"demo\"}}");
            AssertMappedResponse(
                mapper!,
                new LoginResult(LoginStatus.InvalidRequest, null, null, 0),
                400,
                "{\"error\":\"invalid_request\"}");
            AssertMappedResponse(
                mapper!,
                new LoginResult(LoginStatus.InvalidCredentials, null, null, 0),
                401,
                "{\"error\":\"invalid_credentials\"}");
        });
    }

    private static void AssertMappedResponse(
        MethodInfo mapper,
        LoginResult result,
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
        Assert.Equal(expectedJson, JsonSerializer.Serialize(payload, payload!.GetType()));
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

    private static Assembly? ResolveForguncyServerApi(object? sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name);
        return name.Name == "GrapeCity.Forguncy.ServerApi"
            ? Assembly.LoadFrom("D:\\Program Files\\Forguncy 8.0.4\\Website\\bin\\GrapeCity.Forguncy.ServerApi.dll")
            : null;
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

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }

    private sealed record LogEntry(
        LogLevel LogLevel,
        EventId EventId,
        string Message,
        IReadOnlyList<KeyValuePair<string, object?>> State,
        Exception? Exception);
}
