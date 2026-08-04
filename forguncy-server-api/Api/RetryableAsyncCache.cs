namespace ForguncyServerApi.Api;

internal sealed class RetryableAsyncCache<T>
{
    private readonly object gate = new();
    private Lazy<Task<T>>? cachedInitialization;

    public async Task<T> GetOrCreateAsync(Func<Task<T>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        Lazy<Task<T>> initialization;
        lock (gate)
        {
            initialization = cachedInitialization ??= new Lazy<Task<T>>(
                factory,
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        try
        {
            return await initialization.Value;
        }
        catch
        {
            lock (gate)
            {
                if (ReferenceEquals(cachedInitialization, initialization))
                {
                    cachedInitialization = null;
                }
            }

            throw;
        }
    }
}
