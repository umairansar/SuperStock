namespace SuperStock.Utils;

public static class Gatekeeper
{
    public static readonly ManualResetEventSlim Reset = new(false);

    //Alternatives
    //1 https://devblogs.microsoft.com/dotnet/building-async-coordination-primitives-part-1-asyncmanualresetevent/
    //2 https://www.meziantou.net/waiting-for-a-manualreseteventslim-to-be-set-asynchronously.htm
    public static Task WaitAsync(this ManualResetEventSlim manualResetEvent, CancellationToken cancellationToken = default)
    {
        CancellationTokenRegistration cancellationRegistration = default;

        var tcs = new TaskCompletionSource();
        var handle = ThreadPool.RegisterWaitForSingleObject(
            waitObject: manualResetEvent.WaitHandle,
            callBack: (o, timeout) =>
            {
                cancellationRegistration.Unregister();
                tcs.TrySetResult();
            },
            state: null,
            timeout: Timeout.InfiniteTimeSpan,
            executeOnlyOnce: true);

        if (cancellationToken.CanBeCanceled)
        {
            cancellationRegistration = cancellationToken.Register(() =>
            {
                handle.Unregister(manualResetEvent.WaitHandle);
                tcs.TrySetCanceled(cancellationToken);
            });
        }

        return tcs.Task;
    }
}