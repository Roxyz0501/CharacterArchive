using CharacterArchive.Models;
using CharacterArchive.Services;
using CharacterArchive.Windows;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Client.UI;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace CharacterArchive;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/chararchive";
    private readonly WindowSystem windowSystem = new("CharacterArchive");
    private readonly MainWindow mainWindow;
    private readonly LodestoneLookupService lodestoneLookup = new();
    private readonly OwnedOperations operations = new();
    private readonly ConcurrentQueue<System.Action> completions = new();
    private readonly CaptureRetryState captureRetry = new();
    private readonly HashSet<ulong> automaticLookups = [];
    private readonly DeferredConfigurationStore<Configuration> storage;
    private readonly CancellationTokenRegistration shutdownFlush;
    private bool disposed;
    private bool pendingLoginCapture;
    internal string? CaptureError { get; private set; }
    internal bool CaptureStopped => captureRetry.Stopped;
    internal string? SaveError => storage.Error ?? storage.MaintenanceError;
    internal bool SaveStopped => storage.Stopped;
    internal bool RecoveryBlocked => storage.RecoveryBlocked;
    internal bool ConfigurationRecovered => storage.Recovered;
    private int batchLookupRunning;
    private readonly Hook<UIModule.Delegates.HandlePacket> playTimeHook;
    private bool pendingPlayTimeRequest;
    private DateTime playTimeRequestAfter = DateTime.MinValue;
    private DateTime playTimeCommandReadySince = DateTime.MinValue;
    private DateTime lastPlayTimeRequest = DateTime.MinValue;
    private readonly CaptureRetryState playTimeRetry = new();

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] internal static IChatGui Chat { get; private set; } = null!;
    [PluginService] internal static IGameLifecycle GameLifecycle { get; private set; } = null!;

    internal Configuration Configuration { get; }
    internal CharacterRepository Repository { get; }
    internal LocalizationService Localizer { get; }

    public unsafe Plugin()
    {
        storage = new DeferredConfigurationStore<Configuration>(PluginInterface.ConfigFile.FullName,
            () => new Configuration(), ConfigurationCodec.Decode, ConfigurationCodec.Encode);
        Configuration = storage.Value;
        var initializedLanguage = LanguageResolver.Initialize(
            Configuration.DisplayLanguage,
            ClientState.ClientLanguage.ToString(),
            PluginInterface.UiLanguage);
        if (Configuration.Version < 4 || Configuration.DisplayLanguage != initializedLanguage)
        {
            Configuration.DisplayLanguage = initializedLanguage;
            Configuration.Version = 4;
            storage.MarkDirty();
        }
        Configuration.PlayTimeCommandDelaySeconds = Math.Clamp(Configuration.PlayTimeCommandDelaySeconds, 1, 60);
        Configuration.CsvExportDirectory = string.IsNullOrWhiteSpace(Configuration.CsvExportDirectory)
            ? Path.Combine(PluginInterface.GetPluginConfigDirectory(), "exports")
            : Configuration.CsvExportDirectory;

        Localizer = new LocalizationService(Configuration);
        Repository = new CharacterRepository(storage.MarkDirty, Configuration);
        storage.MarkDirty(); // Compare once at the checkpoint; normalized/clamped values may have changed.
        storage.OnStopped = () => Chat.PrintError(Localizer.Text(
            "Saving stopped after three failures. Open Settings and retry saving.",
            "保存が3回失敗したため停止しました。設定から保存を再試行してください。"));
        playTimeHook = GameInteropProvider.HookFromAddress<UIModule.Delegates.HandlePacket>(
            UIModule.StaticVirtualTablePointer->HandlePacket,
            OnUiModulePacket);
        playTimeHook.Enable();
        mainWindow = new MainWindow(this);
        windowSystem.AddWindow(mainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo((_, _) => mainWindow.Toggle())
        {
            HelpMessage = Localizer.Text("Open the Character Archive window.", "Character Archiveのメイン画面を開きます。"),
        });
        PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += mainWindow.Toggle;
        PluginInterface.UiBuilder.OpenConfigUi += mainWindow.Toggle;
        ClientState.Login += OnLogin;
        ClientState.Logout += OnLogout;
        ClientState.ClassJobChanged += OnClassJobChanged;
        ClientState.LevelChanged += OnLevelChanged;
        Framework.Update += OnFrameworkUpdate;
        shutdownFlush = GameLifecycle.GameShuttingDownToken.Register(storage.Flush);

        if (ClientState.IsLoggedIn)
        {
            pendingLoginCapture = true;
            pendingPlayTimeRequest = Configuration.AutoRequestPlayTimeOnLogin;
            playTimeRequestAfter = DateTime.UtcNow;
            if (PlayerState.IsLoaded)
                CaptureCurrentCharacter();
        }
        if (storage.Recovered)
            Chat.Print(Localizer.Text("Configuration restored from a valid backup.", "正常なバックアップから設定を復元しました。"));
        if (storage.Stopped)
            Chat.PrintError(Localizer.Text("Configuration could not be recovered. Restore a valid file and reload the plugin.",
                "設定を復元できませんでした。正常なファイルを戻してプラグインを再読み込みしてください。"));
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        shutdownFlush.Dispose();
        ClientState.Login -= OnLogin;
        ClientState.Logout -= OnLogout;
        ClientState.ClassJobChanged -= OnClassJobChanged;
        ClientState.LevelChanged -= OnLevelChanged;
        Framework.Update -= OnFrameworkUpdate;
        PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= mainWindow.Toggle;
        PluginInterface.UiBuilder.OpenConfigUi -= mainWindow.Toggle;
        CommandManager.RemoveHandler(CommandName);
        playTimeHook.Dispose();
        try
        {
            operations.Dispose(); // Workers never await Framework.Run; cancellation/drain cannot deadlock the UI.
            storage.WithLock(DrainCompletions);
        }
        finally
        {
            try { storage.Dispose(); }
            finally
            {
                windowSystem.RemoveAllWindows();
                lodestoneLookup.Dispose();
            }
        }
    }

    internal bool RequestPlayTime(bool retry = true)
    {
        if (disposed || !ClientState.IsLoggedIn || !PlayerState.IsLoaded)
            return false;

        if (DateTime.UtcNow - lastPlayTimeRequest < TimeSpan.FromSeconds(10))
            return false;

        if (retry) playTimeRetry.Reset();
        if (playTimeRetry.Stopped) return false;
        lastPlayTimeRequest = DateTime.UtcNow; // Rate-limit failed attempts as well as successful commands.

        try
        {
            if (!GameChatCommandService.RequestPlayTime())
                return false;

            playTimeRetry.Reset();
            Log.Information(Localizer.Text(
                "Executed the standard /playtime command.",
                "ゲーム標準の/playtimeコマンドを実行しました"));
            return true;
        }
        catch (Exception exception)
        {
            if (retry)
                Log.Warning(exception, Localizer.Text(
                    "Could not execute the standard /playtime command.",
                    "ゲーム標準の/playtimeコマンドを実行できませんでした"));
            return false;
        }
    }

    internal void SetAutoRequestPlayTimeOnLogin(bool enabled)
    {
        playTimeRetry.Reset();
        Configuration.AutoRequestPlayTimeOnLogin = enabled;
        Repository.SaveSettings();
        pendingPlayTimeRequest = enabled && ClientState.IsLoggedIn;
        playTimeRequestAfter = DateTime.UtcNow;
        playTimeCommandReadySince = DateTime.MinValue;
    }

    private unsafe void OnUiModulePacket(UIModule* thisPtr, UIModulePacketType type, uint uintParam, void* packet)
    {
        playTimeHook.Original(thisPtr, type, uintParam, packet);

        if (disposed || type != UIModulePacketType.PrintPlayTime || packet is null || !PlayerState.IsLoaded)
            return;

        try
        {
            var totalMinutes = unchecked((uint)Marshal.ReadInt32((nint)packet + 0x10));
            storage.WithLock(() => Repository.SetPlayTime(PlayerState.ContentId, totalMinutes));
            Log.Information(Localizer.Text(
                    "Saved playtime: {Minutes} minutes ({ContentId:X16})",
                    "プレイ時間を保存しました: {Minutes}分 ({ContentId:X16})"),
                totalMinutes,
                PlayerState.ContentId);
        }
        catch (Exception exception)
        {
            Log.Warning(exception, Localizer.Text(
                "Could not process the playtime response.",
                "プレイ時間の応答を処理できませんでした"));
        }
    }

    internal void CaptureCurrentCharacter(bool retry = false) => storage.WithLock(() => CaptureCore(retry));

    private void CaptureCore(bool retry)
    {
        if (disposed || !ClientState.IsLoggedIn || storage.RecoveryBlocked)
            return;
        if (retry)
        {
            captureRetry.Reset();
            CaptureError = null;
        }
        if (!captureRetry.CanAttempt(DateTime.UtcNow)) return;
        try
        {
        if (!PlayerState.IsLoaded)
            throw new InvalidDataException("PlayerState is not loaded.");
        var now = DateTime.Now;
        var contentId = PlayerState.ContentId;
        var configPath = CharacterFileLocator.GetFolderPath(contentId);
        var jobs = CaptureAllJobs();
        var record = new CharacterRecord
        {
            ContentId = contentId,
            CharacterName = PlayerState.CharacterName,
            HomeWorld = PlayerState.HomeWorld.IsValid ? PlayerState.HomeWorld.Value.Name.ToString() : string.Empty,
            HomeWorldId = PlayerState.HomeWorld.RowId,
            CurrentWorld = PlayerState.CurrentWorld.IsValid ? PlayerState.CurrentWorld.Value.Name.ToString() : string.Empty,
            CurrentWorldId = PlayerState.CurrentWorld.RowId,
            ConfigFolderName = CharacterFileLocator.GetFolderName(contentId),
            ConfigFolderPath = configPath,
            ConfigFolderExists = Directory.Exists(configPath),
            Race = PlayerState.Race.IsValid ? PlayerState.Race.Value.Masculine.ToString() : string.Empty,
            Tribe = PlayerState.Tribe.IsValid ? PlayerState.Tribe.Value.Masculine.ToString() : string.Empty,
            Sex = PlayerState.Sex.ToString(),
            ClassJob = PlayerState.ClassJob.IsValid ? PlayerState.ClassJob.Value.Name.ToString() : string.Empty,
            CurrentClassJobId = PlayerState.ClassJob.RowId,
            Level = PlayerState.Level,
            Jobs = jobs,
            GrandCompany = PlayerState.GrandCompany.IsValid ? PlayerState.GrandCompany.Value.Name.ToString() : string.Empty,
            GuardianDeity = PlayerState.GuardianDeity.IsValid ? PlayerState.GuardianDeity.Value.Name.ToString() : string.Empty,
            StartTown = PlayerState.StartTown.IsValid ? PlayerState.StartTown.Value.Name.ToString() : string.Empty,
            FirstSeenAt = now,
            LastSeenAt = now,
        };

        // Repository also checks completeness and rejects lost/regressed known job levels.
        var saved = Repository.Upsert(record);
        pendingLoginCapture = false;
        captureRetry.Reset();
        CaptureError = null;
        Log.Information(Localizer.Text(
                "Saved character data: {Name}@{World} ({ContentId:X16})",
                "キャラクター情報を保存しました: {Name}@{World} ({ContentId:X16})"),
            saved.CharacterName,
            saved.HomeWorld,
            saved.ContentId);

        if (Configuration.AutoLookupLodestoneId && string.IsNullOrWhiteSpace(saved.LodestoneId) &&
            automaticLookups.Add(saved.ContentId))
            _ = LookupLodestoneIdAsync(saved);
        }
        catch (Exception ex)
        {
            CaptureError = ex.Message;
            pendingLoginCapture = true;
            if (captureRetry.Fail(DateTime.UtcNow))
            {
                pendingLoginCapture = false;
                var message = Localizer.Text(
                    "Character capture stopped after three failures. Press Refresh Current Character to retry.",
                    "キャラクター取得が3回失敗したため停止しました。「現在のキャラクターを再取得」で再開できます。");
                Log.Warning(ex, message);
                Chat.PrintError(message);
            }
        }
    }

    internal Task LookupLodestoneIdAsync(CharacterRecord record) =>
        operations.Start(record.ContentId, token => LookupLodestoneCoreAsync(record, token));

    private async Task LookupLodestoneCoreAsync(CharacterRecord record, CancellationToken token)
    {
        try
        {
            var result = await lodestoneLookup.FindAsync(record.CharacterName, record.HomeWorld, token).ConfigureAwait(false);
            if (result is null)
            {
                completions.Enqueue(() => mainWindow.SetLookupResult(record.ContentId, Localizer.Text("Not found", "見つかりませんでした")));
                return;
            }

            string? imagePath = null;
            if (!string.IsNullOrWhiteSpace(result.ProfileImageUrl))
            {
                var imageDirectory = Path.Combine(PluginInterface.GetPluginConfigDirectory(), "profile-images");
                var extension = GetSafeImageExtension(result.ProfileImageUrl);
                var destination = Path.Combine(imageDirectory, $"{record.ContentId:X16}{extension}");
                imagePath = record.ProfileImageIsFace && record.ProfileImageUrl == result.ProfileImageUrl &&
                            File.Exists(record.ProfileImagePath) ? record.ProfileImagePath :
                    await lodestoneLookup.DownloadProfileImageAsync(
                    result.ProfileImageUrl,
                    destination,
                    token).ConfigureAwait(false);
            }

            completions.Enqueue(() =>
            {
            Repository.SetLodestoneData(record.ContentId, result.LodestoneId, result.ProfileImageUrl, imagePath);
            mainWindow.SetLookupResult(record.ContentId, Localizer.Text(
                $"Acquired: {result.LodestoneId}",
                $"取得: {result.LodestoneId}"));
            });
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            completions.Enqueue(() =>
            {
            Log.Warning(exception, Localizer.Text(
                "Failed to acquire the Lodestone ID.",
                "Lodestone IDの取得に失敗しました"));
            mainWindow.SetLookupResult(record.ContentId, Localizer.Text("Acquisition failed", "取得に失敗しました"));
            });
        }
    }

    internal bool IsBatchLookupRunning => Volatile.Read(ref batchLookupRunning) != 0;

    internal Task LookupAllLodestoneIdsAsync() => operations.Start(0, LookupAllCoreAsync);

    private async Task LookupAllCoreAsync(CancellationToken token)
    {
        if (Interlocked.Exchange(ref batchLookupRunning, 1) != 0)
            return;

        try
        {
            var targets = Repository.Snapshot()
                .Where(record => string.IsNullOrWhiteSpace(record.LodestoneId) ||
                                 string.IsNullOrWhiteSpace(record.ProfileImagePath) ||
                                 !File.Exists(record.ProfileImagePath) ||
                                 !record.ProfileImageIsFace)
                .ToArray();

            for (var index = 0; index < targets.Length; index++)
            {
                var record = targets[index];
                var position = index + 1;
                completions.Enqueue(() => mainWindow.SetBatchLookupStatus(Localizer.Format(
                    "Batch lookup {0}/{1}: {2}",
                    "まとめて検索中 {0}/{1}: {2}", position, targets.Length, record.CharacterName)));
                token.ThrowIfCancellationRequested();
                await LookupLodestoneIdAsync(record).ConfigureAwait(false);

                if (index + 1 < targets.Length)
                    await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
            }

            completions.Enqueue(() => mainWindow.SetBatchLookupStatus(targets.Length == 0
                ? Localizer.Text("No characters require lookup", "検索対象はありません")
                : Localizer.Format("Batch lookup completed: {0}", "まとめて検索完了: {0}件", targets.Length)));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            completions.Enqueue(() =>
            {
                Log.Warning(exception, Localizer.Text("Failed to acquire the Lodestone ID.", "Lodestone IDの取得に失敗しました"));
                mainWindow.SetBatchLookupStatus(Localizer.Text("Acquisition failed", "取得に失敗しました"));
            });
        }
        finally
        {
            Volatile.Write(ref batchLookupRunning, 0);
        }
    }

    private List<JobLevelRecord> CaptureAllJobs()
    {
        var jobs = new List<JobLevelRecord>();
        foreach (var classJob in DataManager.GetExcelSheet<ClassJob>())
        {
            if (classJob.RowId == 0 || string.IsNullOrWhiteSpace(classJob.Name.ToString()))
                continue;

            jobs.Add(new JobLevelRecord
            {
                RowId = classJob.RowId,
                Name = classJob.Name.ToString(),
                Abbreviation = classJob.Abbreviation.ToString(),
                Level = PlayerState.GetClassJobLevel(classJob),
            });
        }

        return jobs;
    }

    private static string GetSafeImageExtension(string imageUrl)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
            return ".png";

        return Path.GetExtension(uri.AbsolutePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".webp" => Path.GetExtension(uri.AbsolutePath).ToLowerInvariant(),
            _ => ".png",
        };
    }

    private void OnLogin()
    {
        pendingLoginCapture = !captureRetry.Stopped;
        automaticLookups.Clear();
        pendingPlayTimeRequest = Configuration.AutoRequestPlayTimeOnLogin && !playTimeRetry.Stopped;
        playTimeRequestAfter = DateTime.UtcNow;
        playTimeCommandReadySince = DateTime.MinValue;
        CaptureCurrentCharacter();
    }

    private void OnClassJobChanged(uint classJobId) => pendingLoginCapture = !captureRetry.Stopped;

    private void OnLevelChanged(uint classJobId, uint level) => pendingLoginCapture = !captureRetry.Stopped;

    private void OnLogout(int type, int code)
    {
        pendingLoginCapture = false;
        pendingPlayTimeRequest = false;
        storage.WithLock(() => { DrainCompletions(); storage.Flush(); });
    }

    private void DrainCompletions()
    {
        while (completions.TryDequeue(out var action)) action();
    }

    internal void RetrySave() => storage.Retry();
    internal void WithConfigurationLock(System.Action action) => storage.WithLock(action);

    private void OnFrameworkUpdate(IFramework framework) => storage.WithLock(UpdateCore);

    private void UpdateCore()
    {
        if (disposed) return;
        DrainCompletions();
        storage.Tick();
        if (!ClientState.IsLoggedIn)
            return;

        var now = DateTime.UtcNow;
        if (pendingLoginCapture && captureRetry.CanAttempt(now))
        {
            CaptureCurrentCharacter();
        }

        if (pendingPlayTimeRequest && now >= playTimeRequestAfter && PlayerState.IsLoaded)
        {
            if (playTimeRetry.Stopped) { pendingPlayTimeRequest = false; return; }
            if (!GameChatCommandService.CanExecuteTextCommand())
            {
                playTimeCommandReadySince = DateTime.MinValue;
                playTimeRequestAfter = now.AddSeconds(1);
                return;
            }

            if (playTimeCommandReadySince == DateTime.MinValue)
            {
                playTimeCommandReadySince = now;
                return;
            }

            var configuredDelay = TimeSpan.FromSeconds(Math.Clamp(Configuration.PlayTimeCommandDelaySeconds, 1, 60));
            if (now - playTimeCommandReadySince < configuredDelay)
                return;

            if (now - lastPlayTimeRequest < TimeSpan.FromSeconds(10)) return;
            if (RequestPlayTime(retry: false))
            {
                pendingPlayTimeRequest = false;
                playTimeCommandReadySince = DateTime.MinValue;
            }
            else
            {
                playTimeCommandReadySince = DateTime.MinValue;
                playTimeRequestAfter = now.AddSeconds(10);
                if (playTimeRetry.Fail(now))
                {
                    pendingPlayTimeRequest = false;
                    Chat.PrintError(Localizer.Text("Automatic ptime acquisition stopped after three failures. Use Refresh ptime to retry.",
                        "ptime自動取得が3回失敗したため停止しました。「ptime再取得」で再試行してください。"));
                }
            }
        }
    }
}
