using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ForguncyServerApi.Api;

public static class AuthDiagnostics
{
    private const string UnexpectedLoginOperationCode = "auth.login.unexpected_failure";
    private const string UnexpectedRefreshOperationCode = "auth.refresh.unexpected_failure";
    private static readonly EventId UnexpectedLoginEvent = new(1001, "AuthLoginUnexpectedFailure");
    private static readonly EventId UnexpectedRefreshEvent = new(1002, "AuthRefreshUnexpectedFailure");

    public static void RecordLogin(IServiceProvider? services, Exception exception) =>
        Record(services, exception, UnexpectedLoginEvent, UnexpectedLoginOperationCode);

    public static void RecordRefresh(IServiceProvider? services, Exception exception) =>
        Record(services, exception, UnexpectedRefreshEvent, UnexpectedRefreshOperationCode);

    private static void Record(
        IServiceProvider? services,
        Exception exception,
        EventId eventId,
        string operationCode)
    {
        var exceptionType = exception.GetType().Name;

        try
        {
            var logger = services?.GetService(typeof(ILogger<AuthApi>)) as ILogger
                ?? services?.GetService(typeof(ILogger)) as ILogger;
            if (logger is null && services?.GetService(typeof(ILoggerFactory)) is ILoggerFactory loggerFactory)
            {
                logger = loggerFactory.CreateLogger(typeof(AuthApi).FullName ?? nameof(AuthApi));
            }

            if (logger is not null)
            {
                logger.LogError(
                    eventId,
                    "Operation {OperationCode} failed with exception type {ExceptionType}.",
                    operationCode,
                    exceptionType);
                return;
            }
        }
        catch (Exception)
        {
            // Diagnostics must never replace the fixed client error response.
        }

        try
        {
            Trace.TraceError(
                "Operation {0} failed with exception type {1}.",
                operationCode,
                exceptionType);
        }
        catch (Exception)
        {
            // A failing Trace listener must not alter the client response.
        }
    }
}
