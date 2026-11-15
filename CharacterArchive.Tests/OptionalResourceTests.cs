using CharacterArchive.Services;

internal static class OptionalResourceTests
{
    public static void Run(List<string> failures)
    {
        var checks = 0;
        void Check(bool ok, string label) { checks++; if (!ok) failures.Add(label); }
        var reports = 0; var attempts = 0;
        using (var optional = new OptionalResource<Fake>())
        {
            for (var i = 0; i < 100; i++) optional.Initialize(() => { attempts++; throw new OutOfMemoryException("Synthetic allocation failure"); }, _ => { }, _ => reports++);
            Check(optional.Value is null && reports == 1 && attempts == 1, "allocation failure is isolated and attempted/reported only once");
            optional.Dispose();
            Check(optional.Value is null, "failed initialization supports clean disposal");
        }
        var candidate = new Fake();
        using (var optional = new OptionalResource<Fake>())
        {
            optional.Initialize(() => candidate, _ => throw new InvalidOperationException("Synthetic enable failure"), _ => { });
            Check(optional.Value is null && candidate.DisposeCount == 1, "failed activation cleans candidate and clears availability");
        }
        Check(candidate.DisposeCount == 1, "failed candidate is not disposed twice");
        using (var optional = new OptionalResource<Fake>())
        {
            optional.Initialize(() => new Fake { ThrowOnDispose = true }, _ => throw new Exception(), _ => throw new Exception());
            Check(optional.Value is null, "cleanup and logging failures cannot escape initialization");
        }
        var live = new Fake();
        using (var optional = new OptionalResource<Fake>())
        {
            optional.Initialize(() => live, value => Check(ReferenceEquals(optional.Value, value), "original resource is published before activation"), _ => { });
            Check(ReferenceEquals(live, optional.Value), "successful hook remains available");
            optional.Dispose(); optional.Dispose();
            Check(optional.Value is null && live.DisposeCount == 1, "successful resource is disposed once");
            optional.Initialize(() => throw new Exception(), _ => { }, _ => { });
            Check(optional.Value is null, "disposed owner cannot restart initialization");
        }
        Console.WriteLine($"Optional playtime initialization checks passed: {checks}");
    }

    private sealed class Fake : IDisposable
    {
        public int DisposeCount;
        public bool ThrowOnDispose;
        public void Dispose() { DisposeCount++; if (ThrowOnDispose) throw new Exception("Synthetic cleanup failure"); }
    }
}
