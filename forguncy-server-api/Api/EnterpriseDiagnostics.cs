using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ForguncyServerApi.Api;

public static class EnterpriseDiagnostics
{
    private static readonly EventId UnexpectedLoginEvent = new(1101, "EnterpriseLoginUnexpectedFailure");
    private static readonly EventId UnexpectedRefreshEvent = new(1102, "EnterpriseRefreshUnexpectedFailure");
    private static readonly EventId UnexpectedGetInfoEvent = new(1103, "EnterpriseGetInfoUnexpectedFailure");
    private static readonly EventId UnexpectedSendCodeEvent = new(1104, "EnterpriseSendCodeUnexpectedFailure");
    private static readonly EventId UnexpectedVerifyCodeEvent = new(1105, "EnterpriseVerifyCodeUnexpectedFailure");

    public static void RecordLogin(IServiceProvider? services, Exception exception) =>
        Record(services, exception, UnexpectedLoginEvent, "enterprise.login");

    public static void RecordRefresh(IServiceProvider? services, Exception exception) =>
        Record(services, exception, UnexpectedRefreshEvent, "enterprise.refresh");

    public static void RecordGetInfo(IServiceProvider? services, Exception exception) =>
        Record(services, exception, UnexpectedGetInfoEvent, "enterprise.get_info");

    public static void RecordSendCode(IServiceProvider? services, Exception exception) =>
        Record(services, exception, UnexpectedSendCodeEvent, "enterprise.send_code");

    public static void RecordVerifyCode(IServiceProvider? services, Exception exception) =>
        Record(services, exception, UnexpectedVerifyCodeEvent, "enterprise.verify_code");

    private static void Record(
        IServiceProvider? services,
        Exception exception,
        EventId eventId,
        string operationCode)
    {
        var exceptionType = exception.GetType().Name;

        try
        {
            var logger = services?.GetService(typeof(ILogger<EnterpriseApi>)) as ILogger
                ?? services?.GetService(typeof(ILogger)) as ILogger;
            if (logger is null && services?.GetService(typeof(ILoggerFactory)) is ILoggerFactory loggerFactory)
            {
                logger = loggerFactory.CreateLogger(typeof(EnterpriseApi).FullName ?? nameof(EnterpriseApi));
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
        }
    }
}
