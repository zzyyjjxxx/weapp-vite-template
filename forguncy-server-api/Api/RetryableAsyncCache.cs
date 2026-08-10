namespace ForguncyServerApi.Api;

public sealed class RetryableAsyncCache<T>
{
    private readonly object gate = new();
    private Lazy<Task<T>>? cachedInitialization;

    public Task<T> GetOrCreateAsync(Func<Task<T>> factory) =>
        GetOrCreateAsync(factory, CancellationToken.None);

    public async Task<T> GetOrCreateAsync(Func<Task<T>> factory, CancellationToken cancellationToken)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

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
            return await WaitAsync(initializationTask, cancellationToken);
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

    private static async Task<T> WaitAsync(Task<T> task, CancellationToken cancellationToken)
    {
        if (task.IsCompleted || !cancellationToken.CanBeCanceled)
        {
            return await task;
        }

        var cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);
        var completedTask = await Task.WhenAny(task, cancellationTask);
        if (ReferenceEquals(completedTask, cancellationTask))
        {
            throw new OperationCanceledException(cancellationToken);
        }

        return await task;
    }
}
