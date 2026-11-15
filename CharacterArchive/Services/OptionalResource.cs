namespace CharacterArchive.Services;

/// <summary>A failed optional initializer cannot abort the host or leak an enabled candidate.</summary>
internal sealed class OptionalResource<T> : IDisposable where T : class, IDisposable
{
    private bool attempted;
    private bool disposed;
    public T? Value { get; private set; }

    public void Initialize(Func<T> create, Action<T> activate, Action<Exception> report)
    {
        if (attempted || disposed) return;
        attempted = true;
        try
        {
            Value = create();
            activate(Value);
        }
        catch (Exception ex)
        {
            try { Value?.Dispose(); }
            catch { /* Cleanup must not turn optional-feature failure into host startup failure. */ }
            Value = null;
            try { report(ex); }
            catch { /* Logging must not prevent startup either. */ }
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        var value = Value;
        Value = null;
        try { value?.Dispose(); }
        catch { /* The remaining plugin resources must still be released. */ }
    }
}
