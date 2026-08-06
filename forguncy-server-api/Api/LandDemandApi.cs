using ForguncyServerApi.Application;
using GrapeCity.Forguncy.ServerApi;
using Newtonsoft.Json;

namespace ForguncyServerApi.Api;

public class LandDemandApi : ForguncyApi
{
    private static readonly object EnterpriseFactoryOverrideGate = new();
    private static Func<CancellationToken, Task<EnterpriseCompositionRoot>>? enterpriseFactoryOverrideForTests;

    [Get]
    public async Task GetLandDemand()
    {
        var cancellationToken = Context.RequestAborted;
        try
        {
            var enterprise = await GetEnterpriseAsync(cancellationToken);
            var identity = await ReadIdentityOrWriteUnauthorizedAsync(enterprise, cancellationToken);
            if (identity is null)
            {
                return;
            }

            var result = await enterprise.LandDemandService.GetAsync(identity, cancellationToken);
            await WriteResultAsync(result, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LandDemandDiagnostics.RecordGet(Context.RequestServices, exception);
            await WriteErrorAsync(500, "server_error", cancellationToken);
        }
    }

    [Post]
    public async Task AddLandDemand()
    {
        var cancellationToken = Context.RequestAborted;
        try
        {
            var enterprise = await GetEnterpriseAsync(cancellationToken);
            var identity = await ReadIdentityOrWriteUnauthorizedAsync(enterprise, cancellationToken);
            if (identity is null)
            {
                return;
            }

            var request = await ReadRequestOrWriteInvalidAsync(cancellationToken);
            if (request is null)
            {
                return;
            }

            var result = await enterprise.LandDemandService.AddAsync(identity, request, cancellationToken);
            await WriteResultAsync(result, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LandDemandDiagnostics.RecordAdd(Context.RequestServices, exception);
            await WriteErrorAsync(500, "server_error", cancellationToken);
        }
    }

    [Post]
    public async Task UpdateLandDemand()
    {
        var cancellationToken = Context.RequestAborted;
        try
        {
            var enterprise = await GetEnterpriseAsync(cancellationToken);
            var identity = await ReadIdentityOrWriteUnauthorizedAsync(enterprise, cancellationToken);
            if (identity is null)
            {
                return;
            }

            var request = await ReadRequestOrWriteInvalidAsync(cancellationToken);
            if (request is null)
            {
                return;
            }

            var result = await enterprise.LandDemandService.UpdateAsync(identity, request, cancellationToken);
            await WriteResultAsync(result, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LandDemandDiagnostics.RecordUpdate(Context.RequestServices, exception);
            await WriteErrorAsync(500, "server_error", cancellationToken);
        }
    }

    internal static IDisposable PushCompositionRootFactoryOverrideForTests(
        Func<CancellationToken, Task<EnterpriseCompositionRoot>> factory)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        lock (EnterpriseFactoryOverrideGate)
        {
            var previous = enterpriseFactoryOverrideForTests;
            enterpriseFactoryOverrideForTests = factory;
            return new EnterpriseFactoryOverrideScope(previous);
        }
    }

    private Task<EnterpriseCompositionRoot> GetEnterpriseAsync(CancellationToken cancellationToken)
    {
        Func<CancellationToken, Task<EnterpriseCompositionRoot>>? factoryOverride;
        lock (EnterpriseFactoryOverrideGate)
        {
            factoryOverride = enterpriseFactoryOverrideForTests;
        }

        return factoryOverride is not null
            ? factoryOverride(cancellationToken)
            : EnterpriseCompositionRoot.GetOrCreateAsync(DataAccess, cancellationToken);
    }

    private async Task<EnterpriseIdentity?> ReadIdentityOrWriteUnauthorizedAsync(
        EnterpriseCompositionRoot enterprise,
        CancellationToken cancellationToken)
    {
        try
        {
            return await AccessTokenReader.ReadRequiredIdentity(
                Context.Request,
                enterprise.Tokens,
                cancellationToken);
        }
        catch (AccessTokenFormatException)
        {
            await WriteErrorAsync(401, "invalid_token", cancellationToken);
            return null;
        }
    }

    private async Task<LandDemandWriteRequest?> ReadRequestOrWriteInvalidAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            return await LandDemandRequestReader.ReadAsync(Context.Request, cancellationToken);
        }
        catch (LandDemandRequestFormatException)
        {
            await WriteErrorAsync(400, "invalid_request", cancellationToken);
            return null;
        }
    }

    private Task WriteResultAsync(
        LandDemandOperationResult result,
        CancellationToken cancellationToken)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return result.Status switch
        {
            LandDemandOperationStatus.Success when result.Record is not null =>
                ApiResponseWriter.WriteJsonAsync(Context.Response, 200, result.Record, cancellationToken),
            LandDemandOperationStatus.EnterpriseNotFound =>
                WriteErrorAsync(404, "enterprise_not_found", cancellationToken),
            LandDemandOperationStatus.NotFound =>
                WriteErrorAsync(404, "land_demand_not_found", cancellationToken),
            LandDemandOperationStatus.Exists =>
                WriteErrorAsync(409, "land_demand_exists", cancellationToken),
            LandDemandOperationStatus.InvalidRequest =>
                WriteErrorAsync(400, "invalid_request", cancellationToken),
            _ => throw new InvalidOperationException("The land demand operation result is not supported.")
        };
    }

    private Task WriteErrorAsync(int statusCode, string error, CancellationToken cancellationToken) =>
        ApiResponseWriter.WriteJsonAsync(
            Context.Response,
            statusCode,
            new ErrorResponse(error),
            cancellationToken);

    private sealed record ErrorResponse([property: JsonProperty("error")] string Error);

    private sealed class EnterpriseFactoryOverrideScope : IDisposable
    {
        private readonly Func<CancellationToken, Task<EnterpriseCompositionRoot>>? previous;
        private bool disposed;

        public EnterpriseFactoryOverrideScope(
            Func<CancellationToken, Task<EnterpriseCompositionRoot>>? previous)
        {
            this.previous = previous;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            lock (EnterpriseFactoryOverrideGate)
            {
                enterpriseFactoryOverrideForTests = previous;
            }

            disposed = true;
        }
    }
}
