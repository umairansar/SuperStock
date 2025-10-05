namespace SuperStock.Utils;

public static class Throttler
{
    private static readonly SemaphoreSlim _semaphore = new(5);

    public static async Task<T> Run<T>(Func<Task<T>> action)
    {
        Console.WriteLine("Before > {0}", _semaphore.CurrentCount);
        await _semaphore.WaitAsync();

        try
        {
            Console.WriteLine("During > {0}", _semaphore.CurrentCount);
            return await action();
        }
        finally
        {
            _semaphore.Release();
            Console.WriteLine("After > {0}", _semaphore.CurrentCount);
        }
    }
}