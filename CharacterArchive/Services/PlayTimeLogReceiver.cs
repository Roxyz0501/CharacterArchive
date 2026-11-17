namespace CharacterArchive.Services;

internal interface IPlayTimeLog
{
    uint Id { get; }
    int ParameterCount { get; }
    bool TryGetInt(int index, out int value);
}

internal readonly record struct PlayTimeObservation(ulong ContentId, uint Minutes);

/// <summary>Copies only the standard playtime log's scalars while the host event is valid.</summary>
internal sealed class PlayTimeLogReceiver(Func<ulong> currentCharacter, Action<PlayTimeObservation> deliver,
    Action<Exception> stopped) : IDisposable
{
    public const uint PlayTimeLogId = 859;
    private int failures;
    private bool disposed;
    public bool Stopped => failures >= 3;

    // Generic struct adapters avoid boxing/allocating for unrelated log messages.
    public void Receive<T>(T message) where T : IPlayTimeLog
    {
        if (disposed || Stopped) return;
        try
        {
            if (message.Id != PlayTimeLogId) return;
            var contentId = currentCharacter();
            if (contentId == 0) return;
            if (message.ParameterCount != 3 || !message.TryGetInt(0, out var days) ||
                !message.TryGetInt(1, out var hours) || !message.TryGetInt(2, out var minutes) ||
                !TryConvert(days, hours, minutes, out var total))
                throw new InvalidDataException("Unexpected standard playtime log parameters.");
            deliver(new(contentId, total));
            failures = 0;
        }
        catch (Exception ex)
        {
            if (++failures == 3)
            {
                try { stopped(ex); }
                catch { /* No observer or notification failure may escape to the host. */ }
            }
        }
    }

    public static bool TryConvert(int days, int hours, int minutes, out uint total)
    {
        total = 0;
        if (days < 0 || hours is < 0 or > 23 || minutes is < 0 or > 59) return false;
        var value = (ulong)days * 1440 + (uint)hours * 60 + (uint)minutes;
        if (value > uint.MaxValue) return false;
        total = (uint)value;
        return true;
    }

    public void Dispose() => disposed = true;
}

internal sealed class CallbackSubscription(Action detach) : IDisposable
{
    private Action? detach = detach;
    public void Dispose() => Interlocked.Exchange(ref detach, null)?.Invoke();
}
