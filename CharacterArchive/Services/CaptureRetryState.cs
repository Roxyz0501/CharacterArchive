namespace CharacterArchive.Services;

internal sealed class CaptureRetryState
{
    public int Failures { get; private set; }
    public bool Stopped => Failures >= 3;
    public DateTime NextAttempt { get; private set; }
    public bool CanAttempt(DateTime now) => !Stopped && now >= NextAttempt;
    public void Reset() { Failures = 0; NextAttempt = DateTime.MinValue; }
    public bool Fail(DateTime now)
    {
        if (Stopped) return false;
        Failures++;
        NextAttempt = now.AddSeconds(Failures); // Bounded 1s / 2s intervals, then stopped.
        return Stopped;
    }
}
