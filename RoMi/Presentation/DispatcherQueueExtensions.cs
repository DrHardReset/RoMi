using Microsoft.UI.Dispatching;

namespace RoMi.Presentation;

public static class DispatcherQueueExtensions
{
    public static Task EnqueueAsync(this DispatcherQueue queue, Action action)
    {
        TaskCompletionSource tcs = new();
        if (!queue.TryEnqueue(() =>
        {
            try
            {
                action();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }))
        {
            tcs.SetException(new InvalidOperationException("Could not enqueue on DispatcherQueue."));
        }

        return tcs.Task;
    }

    public static Task EnqueueAsync(this DispatcherQueue queue, Func<Task> function)
    {
        TaskCompletionSource tcs = new();
        if (!queue.TryEnqueue(async () =>
        {
            try
            {
                await function();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }))
        {
            tcs.SetException(new InvalidOperationException("Could not enqueue on DispatcherQueue."));
        }

        return tcs.Task;
    }
}
