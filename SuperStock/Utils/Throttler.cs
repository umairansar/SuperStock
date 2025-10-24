namespace SuperStock.Utils;

public static class Throttler
{
    private static readonly SemaphoreSlim _semaphore = new(5);

    public static async Task<T> Run<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        Console.WriteLine("Before > {0}", _semaphore.CurrentCount);
        await _semaphore.WaitAsync(ct);

        try
        {
            Console.WriteLine("During > {0}", _semaphore.CurrentCount);
            return await action(ct);
        }
        finally
        {
            _semaphore.Release();
            Console.WriteLine("After > {0}", _semaphore.CurrentCount);
        }
    }
    
    public static async Task<(bool, T)> TryRun<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        Console.WriteLine("Before > {0}", _semaphore.CurrentCount);
        var tryAcquire = await _semaphore.WaitAsync(5, ct);
        if (!tryAcquire)
        {
            return (false, default)!;
        }
        
        try
        {
            Console.WriteLine("During > {0}", _semaphore.CurrentCount);
            return (true, await action(ct));
        }
        finally
        {
            _semaphore.Release();
            Console.WriteLine("After > {0}", _semaphore.CurrentCount);
        }
    }
}