using System.Reflection;
using System.Runtime.ExceptionServices;
using ForguncyServerApi.Configuration;
using Xunit;

namespace ForguncyServerApi.Tests.Api;

public sealed class RetryableAsyncCacheTests
{
    [Fact]
    public async Task Concurrent_and_later_callers_share_one_successful_initialization()
    {
        var cache = CreateCache<object>();
        var initializationStarted = new TaskCompletionSource<object?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseInitialization = new TaskCompletionSource<object?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = new object();
        var attempts = 0;

        async Task<object> InitializeAsync()
        {
            Interlocked.Increment(ref attempts);
            initializationStarted.SetResult(null);
            await releaseInitialization.Task;
            return expected;
        }

        var first = GetOrCreateAsync(cache, InitializeAsync);
        await initializationStarted.Task;
        var second = GetOrCreateAsync(cache, InitializeAsync);
        releaseInitialization.SetResult(null);

        var concurrentResults = await Task.WhenAll(first, second);
        var laterResult = await GetOrCreateAsync(cache, InitializeAsync);

        Assert.Equal(1, attempts);
        Assert.All(concurrentResults, result => Assert.Same(expected, result));
        Assert.Same(expected, laterResult);
    }

    [Fact]
    public async Task Failed_initialization_is_removed_and_retried()
    {
        var cache = CreateCache<object>();
        var expected = new object();
        var attempts = 0;

        Task<object> InitializeAsync()
        {
            return Interlocked.Increment(ref attempts) == 1
                ? Task.FromException<object>(new InvalidOperationException("synthetic failure"))
                : Task.FromResult(expected);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => GetOrCreateAsync(cache, InitializeAsync));

        Assert.Same(expected, await GetOrCreateAsync(cache, InitializeAsync));
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task Canceled_initialization_is_removed_and_retried()
    {
        var cache = CreateCache<object>();
        var expected = new object();
        var attempts = 0;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Task<object> InitializeAsync()
        {
            return Interlocked.Increment(ref attempts) == 1
                ? Task.FromCanceled<object>(cancellation.Token)
                : Task.FromResult(expected);
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => GetOrCreateAsync(cache, InitializeAsync));

        Assert.Same(expected, await GetOrCreateAsync(cache, InitializeAsync));
        Assert.Equal(2, attempts);
    }

    private static object CreateCache<T>()
    {
        var cacheType = typeof(AuthOptions).Assembly
            .GetType("ForguncyServerApi.Api.RetryableAsyncCache`1", throwOnError: true)!
            .MakeGenericType(typeof(T));
        return Activator.CreateInstance(cacheType, nonPublic: true)!;
    }

    private static async Task<T> GetOrCreateAsync<T>(object cache, Func<Task<T>> factory)
    {
        var method = cache.GetType().GetMethod(
            "GetOrCreateAsync",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        try
        {
            return await Assert.IsAssignableFrom<Task<T>>(method!.Invoke(cache, new object[] { factory }));
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
