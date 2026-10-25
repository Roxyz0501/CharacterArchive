using System.Net;
using System.Net.Http.Headers;
using System.Text;
using CharacterArchive;
using CharacterArchive.Models;
using CharacterArchive.Services;

internal static class ReviewTests
{
    public static async Task Run(List<string> failures)
    {
        var checks = 0;
        void Check(bool ok, string label) { checks++; if (!ok) failures.Add(label); }
        var now = new DateTime(2026, 1, 1);
        var readiness = new CaptureRetryState();
        for (var i = 0; i < 30; i++) readiness.WaitForLoading(now.AddSeconds(i));
        Check(!readiness.Stopped && readiness.Failures == 0 && !readiness.CanAttempt(now.AddSeconds(29)) && readiness.CanAttempt(now.AddSeconds(30)),
            "slow login waits without consuming failure budget or retrying every frame");
        var quests = new[] { new MainScenarioQuest(65537, "Start", "Chapter", []),
            new MainScenarioQuest(65538, "Branch A", "Chapter", [65537]),
            new MainScenarioQuest(65539, "Branch B", "Chapter", [65537]),
            new MainScenarioQuest(65540, "Finish", "Chapter", [65538, 65539]) }.ToDictionary(q => q.Id);
        var terminal = MainScenarioResolver.FindTerminalIds(quests.Values);
        Check(terminal.SequenceEqual([65540u]), "MSQ graph merges branches into one real endpoint");
        MainScenarioProgress? Resolve(ushort[] guide, int selected = 0, uint[]? done = null, uint[]? active = null, uint[]? ends = null) =>
            MainScenarioResolver.Resolve(guide, selected, quests, ends ?? terminal,
                id => (done ?? []).Contains(id), id => (active ?? []).Contains(id), _ => 7, now);
        Check(Resolve([2, 3, 0, 0], 1, active: [65539]) is { QuestId: 65539, Status: MainScenarioStatus.InProgress, Sequence: 7 }, "selected MSQ branch and accepted sequence");
        Check(Resolve([2, 0, 0, 0]) is { Status: MainScenarioStatus.Available, Sequence: null }, "not accepted is not completed");
        Check(Resolve([0, 0, 0, 2], done: [65538]) is { Status: MainScenarioStatus.LastCompleted }, "last-completed guide slot checked against completion");
        Check(Resolve([0, 0, 0, 2]) is null, "unconfirmed completed slot remains unavailable");
        Check(Resolve([0, 0, 0, 0]) is null, "empty guide is never declared completed");
        Check(Resolve([0, 0, 0, 0], done: [65540]) is { Status: MainScenarioStatus.UpToDate }, "verified terminal completion with empty guide");
        Check(Resolve([1, 0, 0, 0], done: [65540], active: [65537]) is { QuestId: 65540, Status: MainScenarioStatus.UpToDate }, "New Game+ does not downgrade completed story");
        Check(Resolve([0, 0, 0, 0], done: [65538, 65539], ends: [65538, 65539]) is null, "ambiguous endpoints cannot prove latest completion");
        Check(Resolve([999, 2, 0], 99) is { QuestId: 65538 }, "invalid guide IDs/index fall back safely");

        var config = new Configuration();
        var dirty = 0;
        var repo = new CharacterRepository(() => dirty++, config);
        var record = new CharacterRecord { ContentId = 1, CharacterName = "Synthetic Character", HomeWorld = "ExampleWorld",
            HomeWorldId = 1, ClassJob = "Synthetic Job", CurrentClassJobId = 1, Level = 10,
            Jobs = [new JobLevelRecord { RowId = 1, Name = "Synthetic Job", Level = 10 }] };
        Check(!repo.SetPlayTime(1, 50), "early playtime reports missing character for buffering");
        repo.Upsert(record);
        var progress = Resolve([2, 0, 0, 0])!;
        repo.SetMainScenario(1, progress);
        var priorDirty = dirty;
        repo.SetMainScenario(1, progress with { ObservedAt = now.AddSeconds(10) });
        Check(dirty == priorDirty, "unchanged MSQ polling causes zero dirty marks");
        repo.Upsert(record);
        Check(repo.Snapshot()[0].MainScenario == progress, "ordinary recapture preserves optional MSQ data");
        Check(ConfigurationCodec.Decode(ConfigurationCodec.Encode(config)).Characters[0].MainScenario == progress, "MSQ persistence roundtrip");
        repo.SetLodestoneData(1, "100", "old-image", "old-path");
        repo.SetLodestoneId(1, "200");
        Check(!repo.Snapshot()[0].ProfileImageIsFace && repo.Snapshot()[0].ProfileImagePath is null, "manual ID changes clear mismatched face");
        Check(!repo.SetLodestoneDataIfUnchanged(1, "100", "300", "new-image", "new-path") && repo.Snapshot()[0].LodestoneId == "200", "late lookup cannot overwrite manual ID edit");
        repo.SetLodestoneData(1, "200", "valid-image", "valid-path");
        repo.SetLodestoneDataIfUnchanged(1, "200", "200", "failed-image", null);
        Check(repo.Snapshot()[0].ProfileImagePath == "valid-path", "failed face download retains matching existing image");
        repo.SetLodestoneDataIfUnchanged(1, "200", "300", null, null);
        Check(repo.Snapshot()[0].LodestoneId == "300" && repo.Snapshot()[0].ProfileImagePath is null, "verified new ID survives absent image without stale face");
        foreach (var language in Enum.GetValues<DisplayLanguage>())
        {
            config.DisplayLanguage = language;
            var localizer = new LocalizationService(config);
            Check(localizer.Text("Uncatalogued fallback", "未登録") == (language == DisplayLanguage.Japanese ? "未登録" : "Uncatalogued fallback"), "real English fallback: " + language);
            Check(!string.IsNullOrWhiteSpace(localizer.Text("Main Scenario", "メインクエスト")), "MSQ localization: " + language);
        }

        var root = Path.Combine(Path.GetTempPath(), "CharacterArchive-review-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var future = Path.Combine(root, "future.json");
            var raw = "{\"Version\":99,\"Characters\":[]}";
            File.WriteAllText(future, raw);
            File.WriteAllText(future + ".bak-00000000000000000001", ConfigurationCodec.Encode(new Configuration()));
            using (var store = new DeferredConfigurationStore<Configuration>(future, () => new(), ConfigurationCodec.Decode, ConfigurationCodec.Encode))
                Check(store.RecoveryBlocked && File.ReadAllText(future) == raw && !store.Recovered, "future config is blocked, never downgraded from backup");
            foreach (var bad in new[] { "{\"ContentId\":1,\"CharacterName\":null,\"HomeWorld\":\"X\"}", "{\"ContentId\":0,\"CharacterName\":\"X\",\"HomeWorld\":\"X\"}" })
            {
                try { ConfigurationCodec.Decode("{\"Version\":4,\"Characters\":[" + bad + "]}"); Check(false, "invalid character rejected"); }
                catch (InvalidDataException) { Check(true, "invalid character rejected"); }
            }
            var clock = new ManualClock();
            var backupPath = Path.Combine(root, "maintenance.json");
            var failPrune = false;
            using (var store = new DeferredConfigurationStore<Configuration>(backupPath, () => new(), ConfigurationCodec.Decode, ConfigurationCodec.Encode, clock,
                step => { if (failPrune && step == "prune") throw new IOException("Synthetic locked backup"); }))
            {
                store.Flush();
                failPrune = true; var notices = 0; store.OnStopped = () => notices++;
                store.Value.PlayTimeCommandDelaySeconds = 2; store.MarkDirty(); store.Flush();
                var writes = store.WriteCount;
                for (var i = 0; i < 10; i++) { clock.Advance(); store.Tick(); }
                Check(store.Stopped && notices == 1 && store.WriteCount == writes, "maintenance retries stop at three without recommitting snapshot");
                failPrune = false; store.Retry();
                Check(!store.Stopped && store.MaintenanceError is null && store.WriteCount == writes, "maintenance explicit recovery without duplicate save");
            }
            var csv = Path.Combine(root, "locked.csv");
            File.WriteAllText(csv, "original");
            using (var locked = new FileStream(csv, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                try { CsvExporter.ExportToPath(repo.Snapshot(), csv); Check(false, "locked CSV rejected"); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Check(true, "locked CSV rejected"); }
            }
            Check(File.ReadAllText(csv) == "original" && Directory.GetFiles(root, "locked.csv.tmp-*").Length == 0, "failed CSV replacement preserves original and cleans own temp");
            var a = CsvExporter.Export([], root); var b = CsvExporter.Export([], root);
            Check(a != b && File.Exists(a) && File.Exists(b), "auto-named CSV exports cannot collide within one second");

            const string name = "Synthetic Character"; const string world = "ExampleWorld";
            static string Card(string id, string n, string w) => $"<a class='entry__link' href='/lodestone/character/{id}/'><p class='entry__name'>{n}</p><p class='entry__world'><i></i>{w} [Example DC]</p></a>";
            var matching = Card("200", name, world);
            Check(LodestoneLookupService.FindExactCharacterId(Card("100", name + " Extra", world) + matching, name, world) == "200", "search selects exact name instead of first hit");
            Check(LodestoneLookupService.FindExactCharacterId(Card("100", name, world + "Extra"), name, world) is null, "world substring not accepted");
            Check(LodestoneLookupService.FindExactCharacterId(matching + Card("201", name, world), name, world) is null, "ambiguous exact hits not guessed");
            Check(LodestoneLookupService.FindExactCharacterId(matching + matching, name, world) == "200", "duplicate markup for one ID is unambiguous");
            Check(LodestoneLookupService.ExtractProfileImageUrl("<meta property='og:image' content='https://img2.finalfantasyxiv.com/logo.png'>") is null, "site OpenGraph image is not a face");
            foreach (var url in new[] { "http://na.finalfantasyxiv.com/", "https://finalfantasyxiv.com.evil.invalid/", "https://user@na.finalfantasyxiv.com/", "https://na.finalfantasyxiv.com:444/" })
                Check(!LodestoneLookupService.IsOfficialUri(new Uri(url)), "unsafe image/redirect host rejected: " + url);
            var handler = new Handler((request, index) => new(HttpStatusCode.OK) { Content = new StringContent(index == 0 ? matching : "<p class='frame__chara__name'>Someone Else</p><p class='frame__chara__world'>ExampleWorld</p>") });
            using (var lookup = new LodestoneLookupService(new HttpClient(handler), new FastClock()))
                Check(await lookup.FindAsync(name, world, default) is null && handler.Count == 2, "profile identity verified after search");
            var requestTimes = new List<long>();
            var verifiedHandler = new Handler((_, index) =>
            {
                requestTimes.Add(System.Diagnostics.Stopwatch.GetTimestamp());
                return new(HttpStatusCode.OK) { Content = new StringContent(index == 0 ? matching :
                    "<p class='frame__chara__name'>Synthetic Character</p><p class='frame__chara__world'>ExampleWorld [Example DC]</p><div class='frame__chara__face'><img src='https://img2.finalfantasyxiv.com/avatar.png'></div>") };
            });
            using (var lookup = new LodestoneLookupService(new HttpClient(verifiedHandler), TimeProvider.System))
            {
                var result = await lookup.FindAsync(name, world, default);
                Check(result is { LodestoneId: "200", ProfileImageUrl: "https://img2.finalfantasyxiv.com/avatar.png" }, "verified exact lookup returns ID and face URL");
                Check(requestTimes.Count == 2 && System.Diagnostics.Stopwatch.GetElapsedTime(requestTimes[0], requestTimes[1]) >= TimeSpan.FromSeconds(.95),
                    "all HTTP requests globally spaced by one second");
            }
            var redirectHandler = new Handler((_, _) => new(HttpStatusCode.Redirect) { Headers = { Location = new Uri("https://evil.invalid/") } });
            using (var lookup = new LodestoneLookupService(new HttpClient(redirectHandler), new FastClock()))
            {
                try { await lookup.FindAsync(name, world, default); Check(false, "external redirect rejected"); }
                catch (InvalidDataException) { Check(redirectHandler.Count == 1, "no request sent to external redirect"); }
            }
            var image = Path.Combine(root, "avatar.png");
            File.WriteAllText(image, "previous-valid-file");
            var imageHandler = new Handler((_, _) => { var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not-image") }; response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png"); return response; });
            using (var lookup = new LodestoneLookupService(new HttpClient(imageHandler), new FastClock()))
                Check(await lookup.DownloadProfileImageAsync("https://img2.finalfantasyxiv.com/avatar.png", image, default) is null && File.ReadAllText(image) == "previous-valid-file", "fake image MIME cannot replace existing file");
            var hugeHandler = new Handler((_, _) => { var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("bounded") }; response.Content.Headers.ContentLength = 3 * 1024 * 1024; return response; });
            using (var lookup = new LodestoneLookupService(new HttpClient(hugeHandler), new FastClock()))
            {
                try { await lookup.FindAsync(name, world, default); Check(false, "oversized HTML rejected"); }
                catch (InvalidDataException) { Check(hugeHandler.Count == 1, "oversized HTML bounded before profile request"); }
            }
            var slow = new BlockingStream();
            var slowHandler = new Handler((_, _) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(slow) };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                return response;
            });
            using (var lookup = new LodestoneLookupService(new HttpClient(slowHandler), new FastClock()))
            using (var owned = new OwnedOperations())
            {
                var task = owned.Start(1, token => lookup.DownloadProfileImageAsync("https://img2.finalfantasyxiv.com/avatar.png", image, token));
                await slow.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
                await Task.Run(owned.Dispose).WaitAsync(TimeSpan.FromSeconds(5));
                Check(task.IsCompletedSuccessfully && owned.Count == 0 && File.ReadAllText(image) == "previous-valid-file" &&
                    Directory.GetFiles(root, "avatar.png.tmp-*").Length == 0, "shutdown cancels stalled HTTP body, drains worker and preserves face");
            }
        }
        finally { Directory.Delete(root, true); }
        Console.WriteLine($"MSQ/review checks executed: {checks}");
    }

    private sealed class Handler(Func<HttpRequestMessage, int, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int Count;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var response = respond(request, Count++); response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }
    private sealed class FastClock : TimeProvider
    {
        private long stamp;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Interlocked.Add(ref stamp, TimeSpan.TicksPerSecond * 2);
    }
    private sealed class ManualClock : TimeProvider
    {
        private long stamp;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => stamp;
        public void Advance() => stamp += TimeSpan.TicksPerSecond * 60;
    }
    private sealed class BlockingStream : Stream
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
            return 0;
        }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
