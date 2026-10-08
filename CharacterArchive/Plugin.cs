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

namespace CharacterArchive;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/chararchive";
    private readonly WindowSystem windowSystem = new("CharacterArchive");
    private readonly MainWindow mainWindow;
    private readonly LodestoneLookupService lodestoneLookup = new();
    private readonly CancellationTokenSource cancellation = new();
    private bool pendingLoginCapture;
    private DateTime nextCaptureAttempt = DateTime.MinValue;
    private int batchLookupRunning;
    private readonly Hook<UIModule.Delegates.HandlePacket> playTimeHook;
    private bool pendingPlayTimeRequest;
    private DateTime playTimeRequestAfter = DateTime.MinValue;
    private DateTime playTimeCommandReadySince = DateTime.MinValue;
    private DateTime lastPlayTimeRequest = DateTime.MinValue;

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

    internal Configuration Configuration { get; }
    internal CharacterRepository Repository { get; }
    internal LocalizationService Localizer { get; }

    public unsafe Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        var initializedLanguage = LanguageResolver.Initialize(
            Configuration.DisplayLanguage,
            ClientState.ClientLanguage.ToString(),
            PluginInterface.UiLanguage);
        if (Configuration.Version < 4 || Configuration.DisplayLanguage != initializedLanguage)
        {
            Configuration.DisplayLanguage = initializedLanguage;
            Configuration.Version = 4;
            PluginInterface.SavePluginConfig(Configuration);
        }
        Configuration.PlayTimeCommandDelaySeconds = Math.Clamp(Configuration.PlayTimeCommandDelaySeconds, 1, 60);
        Configuration.CsvExportDirectory = string.IsNullOrWhiteSpace(Configuration.CsvExportDirectory)
            ? Path.Combine(PluginInterface.GetPluginConfigDirectory(), "exports")
            : Configuration.CsvExportDirectory;

        Localizer = new LocalizationService(Configuration);
        Repository = new CharacterRepository(PluginInterface, Configuration);
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
        ClientState.ClassJobChanged += OnClassJobChanged;
        ClientState.LevelChanged += OnLevelChanged;
        Framework.Update += OnFrameworkUpdate;

        if (ClientState.IsLoggedIn)
        {
            pendingPlayTimeRequest = Configuration.AutoRequestPlayTimeOnLogin;
            playTimeRequestAfter = DateTime.UtcNow;
            if (PlayerState.IsLoaded)
                CaptureCurrentCharacter();
        }
    }

    public void Dispose()
    {
        cancellation.Cancel();
        playTimeHook.Dispose();
        ClientState.Login -= OnLogin;
        ClientState.ClassJobChanged -= OnClassJobChanged;
        ClientState.LevelChanged -= OnLevelChanged;
        Framework.Update -= OnFrameworkUpdate;
        PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= mainWindow.Toggle;
        PluginInterface.UiBuilder.OpenConfigUi -= mainWindow.Toggle;
        CommandManager.RemoveHandler(CommandName);
        windowSystem.RemoveAllWindows();
        lodestoneLookup.Dispose();
        cancellation.Dispose();
    }

    internal bool RequestPlayTime()
    {
        if (!ClientState.IsLoggedIn || !PlayerState.IsLoaded)
            return false;

        if (DateTime.UtcNow - lastPlayTimeRequest < TimeSpan.FromSeconds(10))
            return false;

        try
        {
            if (!GameChatCommandService.RequestPlayTime())
                return false;

            lastPlayTimeRequest = DateTime.UtcNow;
            Log.Information(Localizer.Text(
                "Executed the standard /playtime command.",
                "ゲーム標準の/playtimeコマンドを実行しました"));
            return true;
        }
        catch (Exception exception)
        {
            Log.Warning(exception, Localizer.Text(
                "Could not execute the standard /playtime command.",
                "ゲーム標準の/playtimeコマンドを実行できませんでした"));
            return false;
        }
    }

    internal void SetAutoRequestPlayTimeOnLogin(bool enabled)
    {
        Configuration.AutoRequestPlayTimeOnLogin = enabled;
        Repository.SaveSettings();
        pendingPlayTimeRequest = enabled && ClientState.IsLoggedIn;
        playTimeRequestAfter = DateTime.UtcNow;
        playTimeCommandReadySince = DateTime.MinValue;
    }

    private unsafe void OnUiModulePacket(UIModule* thisPtr, UIModulePacketType type, uint uintParam, void* packet)
    {
        playTimeHook.Original(thisPtr, type, uintParam, packet);

        if (type != UIModulePacketType.PrintPlayTime || packet is null || !PlayerState.IsLoaded)
            return;

        try
        {
            var totalMinutes = unchecked((uint)Marshal.ReadInt32((nint)packet + 0x10));
            Repository.SetPlayTime(PlayerState.ContentId, totalMinutes);
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

    internal void CaptureCurrentCharacter()
    {
        if (!PlayerState.IsLoaded)
        {
            pendingLoginCapture = ClientState.IsLoggedIn;
            return;
        }

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

        var saved = Repository.Upsert(record);
        pendingLoginCapture = !PlayerState.ClassJob.IsValid ||
                              !jobs.Any(job => job.RowId == PlayerState.ClassJob.RowId && job.Level > 0);
        Log.Information(Localizer.Text(
                "Saved character data: {Name}@{World} ({ContentId:X16})",
                "キャラクター情報を保存しました: {Name}@{World} ({ContentId:X16})"),
            saved.CharacterName,
            saved.HomeWorld,
            saved.ContentId);

        if (Configuration.AutoLookupLodestoneId && string.IsNullOrWhiteSpace(saved.LodestoneId))
            _ = LookupLodestoneIdAsync(saved);
    }

    internal async Task LookupLodestoneIdAsync(CharacterRecord record)
    {
        try
        {
            var result = await lodestoneLookup.FindAsync(record.CharacterName, record.HomeWorld, cancellation.Token);
            if (result is null)
            {
                mainWindow.SetLookupResult(record.ContentId, Localizer.Text("Not found", "見つかりませんでした"));
                return;
            }

            string? imagePath = null;
            if (!string.IsNullOrWhiteSpace(result.ProfileImageUrl))
            {
                var imageDirectory = Path.Combine(PluginInterface.GetPluginConfigDirectory(), "profile-images");
                var extension = GetSafeImageExtension(result.ProfileImageUrl);
                var destination = Path.Combine(imageDirectory, $"{record.ContentId:X16}{extension}");
                imagePath = await lodestoneLookup.DownloadProfileImageAsync(
                    result.ProfileImageUrl,
                    destination,
                    cancellation.Token);
            }

            await Framework.Run(
                () => Repository.SetLodestoneData(record.ContentId, result.LodestoneId, result.ProfileImageUrl, imagePath),
                cancellation.Token);
            mainWindow.SetLookupResult(record.ContentId, Localizer.Text(
                $"Acquired: {result.LodestoneId}",
                $"取得: {result.LodestoneId}"));
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Log.Warning(exception, Localizer.Text(
                "Failed to acquire the Lodestone ID.",
                "Lodestone IDの取得に失敗しました"));
            mainWindow.SetLookupResult(record.ContentId, Localizer.Text("Acquisition failed", "取得に失敗しました"));
        }
    }

    internal bool IsBatchLookupRunning => Volatile.Read(ref batchLookupRunning) != 0;

    internal async Task LookupAllLodestoneIdsAsync()
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
                mainWindow.SetBatchLookupStatus(Localizer.Format(
                    "Batch lookup {0}/{1}: {2}",
                    "まとめて検索中 {0}/{1}: {2}", index + 1, targets.Length, record.CharacterName));
                await LookupLodestoneIdAsync(record);

                if (index + 1 < targets.Length)
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellation.Token);
            }

            mainWindow.SetBatchLookupStatus(targets.Length == 0
                ? Localizer.Text("No characters require lookup", "検索対象はありません")
                : Localizer.Format("Batch lookup completed: {0}", "まとめて検索完了: {0}件", targets.Length));
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
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
        pendingLoginCapture = true;
        nextCaptureAttempt = DateTime.MinValue;
        pendingPlayTimeRequest = Configuration.AutoRequestPlayTimeOnLogin;
        playTimeRequestAfter = DateTime.UtcNow;
        playTimeCommandReadySince = DateTime.MinValue;
        CaptureCurrentCharacter();
    }

    private void OnClassJobChanged(uint classJobId) => CaptureCurrentCharacter();

    private void OnLevelChanged(uint classJobId, uint level) => CaptureCurrentCharacter();

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!ClientState.IsLoggedIn)
            return;

        var now = DateTime.UtcNow;
        if (pendingLoginCapture && now >= nextCaptureAttempt)
        {
            nextCaptureAttempt = now.AddSeconds(1);
            CaptureCurrentCharacter();
        }

        if (pendingPlayTimeRequest && now >= playTimeRequestAfter && PlayerState.IsLoaded)
        {
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

            if (RequestPlayTime())
            {
                pendingPlayTimeRequest = false;
                playTimeCommandReadySince = DateTime.MinValue;
            }
            else
            {
                playTimeCommandReadySince = DateTime.MinValue;
                playTimeRequestAfter = now.AddSeconds(1);
            }
        }
    }
}
