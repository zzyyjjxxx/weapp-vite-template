using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ForguncyServerApi.Api;

public static class LandDemandDiagnostics
{
    private static readonly EventId UnexpectedGetEvent = new(1201, "LandDemandGetUnexpectedFailure");
    private static readonly EventId UnexpectedAddEvent = new(1202, "LandDemandAddUnexpectedFailure");
    private static readonly EventId UnexpectedUpdateEvent = new(1203, "LandDemandUpdateUnexpectedFailure");

    public static void RecordGet(IServiceProvider? services, Exception exception) =>
        Record(services, exception, UnexpectedGetEvent, "land_demand.get");

    public static void RecordAdd(IServiceProvider? services, Exception exception) =>
        Record(services, exception, UnexpectedAddEvent, "land_demand.add");

    public static void RecordUpdate(IServiceProvider? services, Exception exception) =>
        Record(services, exception, UnexpectedUpdateEvent, "land_demand.update");

    private static void Record(
        IServiceProvider? services,
        Exception exception,
        EventId eventId,
        string operationCode)
    {
        var exceptionType = exception.GetType().Name;

        try
        {
            var logger = services?.GetService(typeof(ILogger<LandDemandApi>)) as ILogger
                ?? services?.GetService(typeof(ILogger)) as ILogger;
            if (logger is null && services?.GetService(typeof(ILoggerFactory)) is ILoggerFactory loggerFactory)
            {
                logger = loggerFactory.CreateLogger(typeof(LandDemandApi).FullName ?? nameof(LandDemandApi));
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
