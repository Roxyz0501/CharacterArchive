using CharacterArchive;
using CharacterArchive.Models;
using CharacterArchive.Services;

internal static class PersistenceTests
{
    public static async Task Run(List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "CharacterArchive-persistence-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var checks = 0;
        void Check(bool value, string message) { checks++; if (!value) failures.Add(message); }
        DeferredConfigurationStore<Configuration> Open(string path, Clock clock, Action<string>? fault = null) =>
            new(path, () => new Configuration(), ConfigurationCodec.Decode, ConfigurationCodec.Encode, clock, fault);
        try
        {
            var path = Path.Combine(root, "normal.json");
            var clock = new Clock();
            var baseline = new Configuration { DisplayLanguage = DisplayLanguage.Japanese };
            File.WriteAllText(path, ConfigurationCodec.Encode(baseline));
            using (var store = Open(path, clock))
            {
                for (var i = 0; i < 8; i++) { clock.Advance(60); store.Tick(); }
                Check(store.WriteCount == 0 && store.LogicalBytes == 0, "idle: zero writes/bytes over eight periods");
                for (var i = 0; i < 600; i++) { store.Value.PlayTimeCommandDelaySeconds = i + 2; store.MarkDirty(); store.Tick(); }
                Check(store.WriteCount == 0, "600 input events: no immediate writes");
                clock.Advance(60); store.Tick();
                Check(store.WriteCount == 1, "600 input events: one checkpoint");
                Console.WriteLine($"Persistence input probe: events=600, saves={store.WriteCount}, logicalBytes={store.LogicalBytes}");
                var last = store.Value.PlayTimeCommandDelaySeconds;
                store.Value.PlayTimeCommandDelaySeconds = 20; store.MarkDirty();
                store.Value.PlayTimeCommandDelaySeconds = last; store.MarkDirty();
                clock.Advance(60); store.Tick();
                Check(store.WriteCount == 1, "change then revert: zero extra writes");
                for (var i = 2; i <= 6; i++) { store.Value.PlayTimeCommandDelaySeconds = i; store.MarkDirty(); store.Flush(); }
                Check(Directory.GetFiles(root, "normal.json.bak-*").Length == 3, "three backup generations");
                var generations = Directory.GetFiles(root, "normal.json.bak-*").OrderDescending().Select(p => ConfigurationCodec.Decode(File.ReadAllText(p)).PlayTimeCommandDelaySeconds).ToArray();
                Check(generations.SequenceEqual([5, 4, 3]), "backups contain three previous valid snapshots");
                store.Value.PlayTimeCommandDelaySeconds = 9; store.MarkDirty();
                await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(store.Flush)));
                Check(store.WriteCount == 7, "concurrent flushes coalesce to one write");
                store.Value.PlayTimeCommandDelaySeconds = 10; store.MarkDirty(); store.Dispose();
                Check(ConfigurationCodec.Decode(File.ReadAllText(path)).PlayTimeCommandDelaySeconds == 10, "dispose flushes pending value");
                var bytes = store.LogicalBytes;
                store.Value.PlayTimeCommandDelaySeconds = 11; store.MarkDirty(); clock.Advance(120); store.Tick(); store.Flush(); store.Dispose();
                Check(store.LogicalBytes == bytes && store.WriteCount == 8, "after Dispose: zero writes");
            }
            var newest = Directory.GetFiles(root, "normal.json.bak-*").OrderDescending().First();
            File.WriteAllText(newest, "broken newest backup");
            File.WriteAllText(path, "broken primary");
            using (var recovered = Open(path, clock))
            {
                Check(recovered.Recovered && recovered.Value.PlayTimeCommandDelaySeconds == 6, "corrupt primary/latest backup: recover next valid generation");
                Check(Directory.GetFiles(root, "normal.json.corrupt-*").Any(p => File.ReadAllText(p) == "broken primary"), "corrupt primary retained");
            }
            using (var healthy = Open(path, clock))
                Check(!healthy.Recovered && healthy.WriteCount == 0, "healthy main wins without rewriting");

            foreach (var stage in new[] { "write", "validate", "replace" })
            {
                var failurePath = Path.Combine(root, stage + ".json");
                File.WriteAllText(failurePath, ConfigurationCodec.Encode(baseline));
                var injected = true;
                var attempts = 0;
                using var broken = Open(failurePath, clock, point =>
                {
                    if (point == stage && injected) { attempts++; throw new IOException("Injected " + stage); }
                });
                var notifications = 0;
                broken.OnStopped = () => notifications++;
                broken.Value.PlayTimeCommandDelaySeconds = 7; broken.MarkDirty();
                for (var i = 0; i < 100; i++) { clock.Advance(60); broken.Tick(); }
                Check(attempts == 3 && notifications == 1 && broken.Stopped, stage + ": stop after three attempts, notify once");
                Check(File.ReadAllText(failurePath) == ConfigurationCodec.Encode(baseline) && Directory.GetFiles(root, stage + ".json.bak-*").Length == 0,
                    stage + ": original/backup generations unchanged");
                Check(Directory.GetFiles(root, stage + ".json.tmp-*").Length == 0, stage + ": no leftover temporary file");
                injected = false; broken.Retry();
                Check(!broken.Stopped && broken.WriteCount == 1 && ConfigurationCodec.Decode(File.ReadAllText(failurePath)).PlayTimeCommandDelaySeconds == 7,
                    stage + ": explicit retry commits retained pending data");
                Console.WriteLine($"Fault probe {stage}: attempts={attempts}, notifications={notifications}, commits={broken.WriteCount}, logicalBytesIncludingFailedTemps={broken.LogicalBytes}");
            }

            var changingPath = Path.Combine(root, "change-during-save.json");
            var lockedPath = Path.Combine(root, "locked.json");
            File.WriteAllText(lockedPath, ConfigurationCodec.Encode(baseline));
            using (var locked = Open(lockedPath, clock))
            {
                locked.Value.PlayTimeCommandDelaySeconds = 12; locked.MarkDirty();
                using (var held = new FileStream(lockedPath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    for (var i = 0; i < 3; i++) { clock.Advance(60); locked.Tick(); }
                    Check(locked.Stopped && locked.WriteCount == 0, "real Windows exclusive file lock: bounded stop");
                }
                locked.Retry();
                Check(ConfigurationCodec.Decode(File.ReadAllText(lockedPath)).PlayTimeCommandDelaySeconds == 12,
                    "lock released: explicit retry recovers");
            }
            var damagedTempPath = Path.Combine(root, "damaged-temp.json");
            File.WriteAllText(damagedTempPath, ConfigurationCodec.Encode(baseline));
            using (var damaged = Open(damagedTempPath, clock, point =>
            {
                if (point == "validate")
                    File.WriteAllText(Directory.GetFiles(root, "damaged-temp.json.tmp-*").Single(), "truncated temporary JSON");
            }))
            {
                damaged.Value.PlayTimeCommandDelaySeconds = 13; damaged.MarkDirty(); damaged.Flush();
                Check(damaged.WriteCount == 0 && File.ReadAllText(damagedTempPath) == ConfigurationCodec.Encode(baseline),
                    "temporary validation rejects damaged JSON before replacing main");
            }
            File.WriteAllText(changingPath, ConfigurationCodec.Encode(baseline));
            DeferredConfigurationStore<Configuration>? changing = null;
            var mutate = true;
            changing = Open(changingPath, clock, point =>
            {
                if (point == "replace" && mutate)
                {
                    mutate = false; changing!.Value.PlayTimeCommandDelaySeconds = 8; changing.MarkDirty();
                }
            });
            using (changing)
            {
                changing.Value.PlayTimeCommandDelaySeconds = 3; changing.MarkDirty(); changing.Flush();
                Check(ConfigurationCodec.Decode(File.ReadAllText(changingPath)).PlayTimeCommandDelaySeconds == 3, "first in-flight snapshot stays consistent");
                clock.Advance(60); changing.Tick();
                Check(changing.WriteCount == 2 && ConfigurationCodec.Decode(File.ReadAllText(changingPath)).PlayTimeCommandDelaySeconds == 8, "change during save stays dirty");
            }

            var corruptPath = Path.Combine(root, "unrecoverable.json");
            File.WriteAllText(corruptPath, "not-json");
            using (var blocked = Open(corruptPath, clock))
            {
                blocked.MarkDirty(); blocked.Flush(); blocked.Retry();
                Check(blocked.RecoveryBlocked && blocked.WriteCount == 0 && File.ReadAllText(corruptPath) == "not-json", "never overwrite unrecoverable data with defaults");
            }
            var legacy = ConfigurationCodec.Decode("{\"$type\":\"CharacterArchive.Configuration, CharacterArchive\",\"Version\":4,\"Characters\":[],\"DisplayLanguage\":2}");
            Check(legacy.DisplayLanguage == DisplayLanguage.Japanese, "legacy Newtonsoft object metadata remains readable");
            var wrongTypePath = Path.Combine(root, "wrong-type.json");
            File.WriteAllText(wrongTypePath, "{\"Version\":\"invalid\",\"Characters\":[]}");
            File.WriteAllText(wrongTypePath + ".bak-00000000000000000001", ConfigurationCodec.Encode(baseline));
            using (var recovered = Open(wrongTypePath, clock))
                Check(recovered.Recovered && recovered.Value.DisplayLanguage == DisplayLanguage.Japanese,
                    "invalid version type recovers from a valid backup");

            var snapshotPath = Path.Combine(root, "profiles.json");
            using (var store = Open(snapshotPath, clock))
            {
                var repository = new CharacterRepository(store.MarkDirty, store.Value);
                var good = Record(); repository.Upsert(good); store.Flush();
                var before = File.ReadAllText(snapshotPath);
                var retry = new CaptureRetryState(); var notifications = 0;
                for (var i = 0; i < 100; i++)
                {
                    var now = clock.GetUtcNow().UtcDateTime;
                    if (retry.CanAttempt(now))
                    {
                        try { var bad = Record(); bad.Jobs.Clear(); repository.Upsert(bad); }
                        catch (InvalidDataException) { if (retry.Fail(now)) notifications++; }
                    }
                    clock.Advance(1);
                }
                store.Flush();
                Check(retry.Failures == 3 && notifications == 1 && retry.Stopped, "incomplete capture stops after three failures");
                Check(store.WriteCount == 1 && File.ReadAllText(snapshotPath) == before && repository.Snapshot()[0].Jobs[0].Level == 90,
                    "incomplete capture cannot overwrite valid profile or mark dirty");
                var regressed = Record(); regressed.Jobs.Add(new JobLevelRecord { RowId = 2, Level = 50 }); repository.Upsert(regressed);
                try { repository.Upsert(Record()); Check(false, "lost old job should reject snapshot"); } catch (InvalidDataException) { Check(true, "lost old job rejected"); }
                retry.Reset();
                regressed.CharacterName = "Updated Synthetic"; repository.Upsert(regressed);
                Check(retry.CanAttempt(clock.GetUtcNow().UtcDateTime), "manual capture retry reopens gate");
                clock.SetWallTime(new DateTimeOffset(2026, 10, 22, 23, 59, 59, TimeSpan.Zero));
                clock.Advance(60); store.Tick();
                clock.SetWallTime(new DateTimeOffset(2026, 10, 21, 23, 0, 0, TimeSpan.Zero));
                repository.SetLodestoneId(1, "synthetic-id"); clock.Advance(60); store.Tick();
                Check(ConfigurationCodec.Decode(File.ReadAllText(snapshotPath)).Characters.Single().LodestoneId == "synthetic-id", "midnight and wall-clock rollback do not stall monotonic checkpoints");
                Check(Directory.GetDirectories(root).Length == 0, "profiles remain snapshots, no daily history generated");
            }

            using (var operations = new OwnedOperations())
            {
                var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var calls = 0;
                var first = operations.Start(1, async token => { Interlocked.Increment(ref calls); started.SetResult(); await release.Task.WaitAsync(token); });
                await started.Task;
                var peers = Enumerable.Range(0, 100).Select(_ => operations.Start(1, _ => throw new Exception("duplicate"))).ToArray();
                Check(peers.All(t => ReferenceEquals(first, t)) && calls == 1, "100 same-ContentId calls share one operation");
                var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _ = operations.Start(2, async token => { secondStarted.SetResult(); await Task.Delay(Timeout.InfiniteTimeSpan, token); });
                await secondStarted.Task;
                await Task.Run(operations.Dispose);
                Check(first.IsCompleted && operations.Count == 0, "shutdown cancels and drains every owned worker");
                await operations.Start(3, _ => throw new Exception("accepted after shutdown"));
                Check(operations.Count == 0, "shutdown rejects new work");
            }

            var latePath = Path.Combine(root, "late-completion.json");
            var late = Open(latePath, clock);
            var lateRepo = new CharacterRepository(late.MarkDirty, late.Value);
            lateRepo.Upsert(Record());
            var queue = new System.Collections.Concurrent.ConcurrentQueue<Action>();
            using (var work = new OwnedOperations())
            {
                var queued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _ = work.Start(1, async token =>
                {
                    queue.Enqueue(() => lateRepo.SetLodestoneId(1, "late-synthetic-result"));
                    queued.SetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                });
                await queued.Task;
                work.Dispose();
                while (queue.TryDequeue(out var completion)) late.WithLock(completion);
                late.Dispose();
                Check(ConfigurationCodec.Decode(File.ReadAllText(latePath)).Characters.Single().LodestoneId == "late-synthetic-result",
                    "shutdown drains completed lookup result before final configuration flush");
                Check(late.WriteCount == 1 && work.Count == 0 && queue.IsEmpty, "shutdown: one flush, no owned workers/completions remain");
            }
        }
        catch (Exception ex) { failures.Add("Persistence test exception: " + ex); }
        finally { Directory.Delete(root, true); }
        Console.WriteLine($"Persistence/lifecycle checks executed: {checks}");
    }

    private static CharacterRecord Record() => new()
    {
        ContentId = 1, CharacterName = "Synthetic Character", HomeWorld = "Synthetic World", HomeWorldId = 1,
        CurrentClassJobId = 1, ClassJob = "Synthetic Job", Level = 90,
        Jobs = [new JobLevelRecord { RowId = 1, Level = 90 }],
    };

    private sealed class Clock : TimeProvider
    {
        private long ticks;
        private DateTimeOffset wall = new(2026, 10, 22, 0, 0, 0, TimeSpan.Zero);
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => ticks;
        public override DateTimeOffset GetUtcNow() => wall;
        public void SetWallTime(DateTimeOffset value) => wall = value;
        public void Advance(int seconds) { ticks += TimeSpan.FromSeconds(seconds).Ticks; wall = wall.AddSeconds(seconds); }
    }
}
