using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ForguncyServerApi.Api;

public static class AuthDiagnostics
{
    private const string UnexpectedLoginOperationCode = "auth.login.unexpected_failure";
    private static readonly EventId UnexpectedLoginEvent = new(1001, "AuthLoginUnexpectedFailure");

    public static void Record(IServiceProvider? services, Exception exception)
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
                    UnexpectedLoginEvent,
                    "Operation {OperationCode} failed with exception type {ExceptionType}.",
                    UnexpectedLoginOperationCode,
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
                UnexpectedLoginOperationCode,
                exceptionType);
        }
        catch (Exception)
        {
            // A failing Trace listener must not alter the client response.
        }
    }
}
