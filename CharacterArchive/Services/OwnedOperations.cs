namespace CharacterArchive.Services;

/// <summary>Keyed single-flight workers. Workers must not await the framework/UI thread.</summary>
internal sealed class OwnedOperations : IDisposable
{
    private readonly object gate = new();
    private readonly Dictionary<ulong, Task> running = new();
    private readonly CancellationTokenSource cancellation = new();
    private bool stopped;
    public int Count { get { lock (gate) return running.Count; } }

    public Task Start(ulong key, Func<CancellationToken, Task> operation)
    {
        lock (gate)
        {
            if (stopped) return Task.CompletedTask;
            if (running.TryGetValue(key, out var existing)) return existing;
            var task = Task.Run(async () =>
            {
                try { await operation(cancellation.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
                finally { lock (gate) running.Remove(key); }
            });
            running.Add(key, task);
            return task;
        }
    }

    public void Dispose()
    {
        Task[] tasks;
        lock (gate)
        {
            if (stopped) return;
            stopped = true;
            tasks = running.Values.ToArray();
        }
        cancellation.Cancel();
        try { Task.WhenAll(tasks).GetAwaiter().GetResult(); }
        finally { cancellation.Dispose(); }
    }
}
