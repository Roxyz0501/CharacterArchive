using System.Text;

namespace CharacterArchive.Services;

/// <summary>One writer, monotonic 60-second checkpoints, immutable valid backup generations.</summary>
internal sealed class DeferredConfigurationStore<T> : IDisposable where T : class
{
    private readonly object gate = new();
    private readonly string path;
    private readonly Func<string, T> decode;
    private readonly Func<T, string> encode;
    private readonly TimeProvider clock;
    private readonly Action<string>? fault;
    private long lastCheck;
    private long revision;
    private long savedRevision;
    private long backupSequence;
    private string? saved;
    private bool disposed;
    private bool recoveryBlocked;
    private int failures;
    private int cleanupFailures;

    public T Value { get; }
    public bool Stopped => recoveryBlocked || failures >= 3 || cleanupFailures >= 3;
    public bool RecoveryBlocked => recoveryBlocked;
    public string? Error { get; private set; }
    public string? MaintenanceError { get; private set; }
    public bool Recovered { get; private set; }
    public long WriteCount { get; private set; }
    public long LogicalBytes { get; private set; }
    public Action? OnStopped { get; set; }

    public DeferredConfigurationStore(string path, Func<T> create, Func<string, T> decode,
        Func<T, string> encode, TimeProvider? clock = null, Action<string>? fault = null)
    {
        this.path = Path.GetFullPath(path);
        this.decode = decode;
        this.encode = encode;
        this.clock = clock ?? TimeProvider.System;
        this.fault = fault;
        lastCheck = this.clock.GetTimestamp();
        Value = create();
        try
        {
            var backups = Backups();
            backupSequence = backups.Select(Sequence).DefaultIfEmpty(0).Max();
            if (TryLoad(this.path, out var main))
            {
                Value = main!;
                saved = encode(Value);
                return;
            }
            foreach (var backup in backups)
            {
                if (!TryLoad(backup, out var candidate)) continue;
                Value = candidate!;
                // Replace keeps the broken original as evidence; never promote it to a valid backup.
                var text = encode(Value);
                Install(text, true);
                saved = text;
                Recovered = true;
                return;
            }
            if (Exists(this.path) || backups.Length != 0)
                throw new InvalidDataException("No valid configuration or backup. Restore a valid file and reload the plugin.");
            revision = 1; // First run: initialized settings are saved by the first checkpoint/flush.
        }
        catch (Exception ex)
        {
            recoveryBlocked = true;
            Error = ex.Message;
        }
    }

    public void MarkDirty() { lock (gate) { if (!disposed) revision++; } }
    public void WithLock(Action action) { lock (gate) { if (!disposed) action(); } }

    public void Tick()
    {
        lock (gate)
        {
            if (disposed || clock.GetElapsedTime(lastCheck) < TimeSpan.FromSeconds(60)) return;
            lastCheck = clock.GetTimestamp();
            FlushUnsafe();
        }
    }

    public void Flush() { lock (gate) { if (!disposed) FlushUnsafe(); } }

    public void Retry()
    {
        lock (gate)
        {
            if (disposed || recoveryBlocked) return;
            failures = 0;
            cleanupFailures = 0;
            Error = null;
            lastCheck = clock.GetTimestamp();
            if (MaintenanceError is not null) PruneBackups();
            FlushUnsafe();
        }
    }

    private void FlushUnsafe()
    {
        if (Stopped || revision == savedRevision) return;
        var snapshotRevision = revision;
        try
        {
            var text = encode(Value);
            if (text != saved)
            {
                decode(text); // Validate the complete snapshot before touching the disk.
                Install(text, false);
                saved = text;
            }
            savedRevision = snapshotRevision; // A change during writing stays dirty.
            failures = 0;
            Error = null;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            if (++failures == 3) NotifyStopped();
        }
    }

    private void Install(string text, bool restoring)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            fault?.Invoke("write");
            var bytes = Encoding.UTF8.GetBytes(text);
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                LogicalBytes += bytes.Length;
                stream.Flush(true);
            }
            fault?.Invoke("validate");
            decode(File.ReadAllText(temporary));
            string? backup = null;
            if (Exists(path))
            {
                var mainIsValid = TryLoad(path, out _);
                if (!mainIsValid && !restoring)
                    throw new InvalidDataException("The current configuration became invalid. Restore it before retrying.");
                backup = mainIsValid
                    ? path + ".bak-" + (++backupSequence).ToString("D20")
                    : path + ".corrupt-" + Guid.NewGuid().ToString("N");
            }
            fault?.Invoke("replace");
            if (backup is null) File.Move(temporary, path);
            else File.Replace(temporary, path, backup);
            WriteCount++;
            // The commit succeeded. Cleanup failure must never cause another commit of this snapshot.
            PruneBackups();
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private string[] Backups()
    {
        var directory = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(directory)) return [];
        return Directory.GetFiles(directory, Path.GetFileName(path) + ".bak-*")
            .Where(p => Sequence(p) > 0).OrderByDescending(Sequence).ToArray();
    }

    private void PruneBackups()
    {
        try
        {
            foreach (var stale in Backups().Where(p => TryLoad(p, out _)).Skip(3)) File.Delete(stale);
            cleanupFailures = 0;
            MaintenanceError = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MaintenanceError = ex.Message;
            if (++cleanupFailures == 3) NotifyStopped();
        }
    }

    private void NotifyStopped()
    {
        try { OnStopped?.Invoke(); }
        catch { /* A host notification failure must not restart writing or prevent resource cleanup. */ }
    }

    private static long Sequence(string file) => long.TryParse(file[(file.LastIndexOf(".bak-", StringComparison.Ordinal) + 5)..], out var number) ? number : 0;

    private bool TryLoad(string file, out T? value)
    {
        value = null;
        if (!Exists(file)) return false;
        try { value = decode(File.ReadAllText(file)); return true; }
        catch (System.Text.Json.JsonException) { return false; }
        catch (InvalidDataException) { return false; }
    }

    private static bool Exists(string file)
    {
        try { return (File.GetAttributes(file) & FileAttributes.Directory) == 0; }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            FlushUnsafe();
            disposed = true;
        }
    }
}
