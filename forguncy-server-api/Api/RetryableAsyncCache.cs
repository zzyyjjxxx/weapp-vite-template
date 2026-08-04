namespace ForguncyServerApi.Api;

internal sealed class RetryableAsyncCache<T>
{
    private readonly object gate = new();
    private Lazy<Task<T>>? cachedInitialization;

    public Task<T> GetOrCreateAsync(Func<Task<T>> factory) =>
        GetOrCreateAsync(factory, CancellationToken.None);

    public async Task<T> GetOrCreateAsync(Func<Task<T>> factory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(factory);

        Lazy<Task<T>> initialization;
        lock (gate)
        {
            initialization = cachedInitialization ??= new Lazy<Task<T>>(
                factory,
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        Task<T> initializationTask;
        try
        {
            initializationTask = initialization.Value;
        }
        catch
        {
            RemoveIfCurrent(initialization);
            throw;
        }

        try
        {
            return await initializationTask.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested
            && !initializationTask.IsCanceled
            && !initializationTask.IsFaulted)
        {
            throw;
        }
        catch
        {
            RemoveIfCurrent(initialization);
            throw;
        }
    }

    private void RemoveIfCurrent(Lazy<Task<T>> initialization)
    {
        lock (gate)
        {
            if (ReferenceEquals(cachedInitialization, initialization))
            {
                cachedInitialization = null;
            }
        }
    }
}
