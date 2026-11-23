using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using CharacterArchive.Models;
using CharacterArchive.Services;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Roxyz0501.Dalamud.CommonUi;

namespace CharacterArchive.Windows;

public sealed class MainWindow : Window
{
    private readonly Plugin plugin;
    private readonly ConcurrentDictionary<ulong, string> lookupResults = new();
    private readonly FileDialogManager fileDialogManager = new();
    private string statusMessage = string.Empty;
    private string batchLookupStatus = string.Empty;
    private long nextRelogClick;
    private readonly StandardPluginInfo dependencyInfo = new(new PluginDependency("AutoRetainer", "AutoRetainer", false,
        new("Character switching", "キャラクター切替", "Charakterwechsel", "Changement de personnage", "캐릭터 전환", "角色切换", "角色切換"),
        new("Register the target in AutoRetainer first.", "切替先のキャラクターはAutoRetainer側にも登録が必要です。", "Ziel vorher in AutoRetainer registrieren.", "Enregistrez la cible dans AutoRetainer.", "대상을 AutoRetainer에 먼저 등록하세요.", "请先在 AutoRetainer 注册目标角色。", "請先在 AutoRetainer 登錄目標角色。"),
        "https://github.com/PunishXIV/AutoRetainer"));
    private string InfoLanguage => ToLanguageCode(plugin.Configuration.DisplayLanguage ?? DisplayLanguage.English);
    private ulong sortedPin;
    private long sortedRevision = -1;
    private int sortedColumn = -1;
    private bool sortedDescending;
    private CharacterRecord[] sortedOverview = [];
    private long snapshotRevision = -1;
    private IReadOnlyList<CharacterRecord> snapshot = [];
    private readonly HashSet<string> failedImages = new(StringComparer.OrdinalIgnoreCase);

    public MainWindow(Plugin plugin)
        : base("Character Archive##CharacterArchiveMain")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(720, 430),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw() => plugin.WithConfigurationLock(DrawContents);

    private void DrawContents()
    {
        fileDialogManager.Draw();
        if (snapshotRevision != plugin.Repository.Revision)
        {
            snapshot = plugin.Repository.Snapshot();
            snapshotRevision = plugin.Repository.Revision;
        }
        var records = snapshot;
        DrawHeader(records.Count);

        if (!ImGui.BeginTabBar("CharacterArchiveTabs"))
            return;

        if (ImGui.BeginTabItem($"{T("Character Overview", "キャラクター概要")}###Overview"))
        {
            DrawOverview(records);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem($"{T("Character Details", "キャラクター詳細")}###Details"))
        {
            DrawDetails(records);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem($"{T("Character File Mapping", "キャラファイル紐づけ")}###Mapping"))
        {
            DrawMapping(records);
            ImGui.EndTabItem();
        }

        StandardPluginTabs.DrawStandardTrailingTabs(
            T("Settings", "設定"),
            "Settings",
            DrawSettings,
            T("Language settings", "言語設定"),
            DrawLanguageSettings,
            T("Support", "支援"),
            DrawSupport, StandardPluginInfo.Title(InfoLanguage), () => dependencyInfo.Draw(Plugin.PluginInterface, InfoLanguage));

        ImGui.EndTabBar();
    }

    internal void SetLookupResult(ulong contentId, string message)
    {
        lookupResults[contentId] = message;
        failedImages.Clear();
    }

    internal void SetBatchLookupStatus(string message) => batchLookupStatus = message;

    private void DrawHeader(int count)
    {
        ImGui.Text(F("Saved characters: {0}", "保存済みキャラクター: {0}", count));
        ImGui.SameLine();
        if (ImGui.Button(T("Refresh Current Character", "現在のキャラクターを再取得")))
            plugin.CaptureCurrentCharacter(retry: true);
        ImGui.SameLine();
        if (ImGui.Button(T("Refresh ptime", "ptime再取得")))
            statusMessage = !plugin.PlayTimeAvailable
                ? T("Playtime acquisition is unavailable. Reload the plugin to retry.", "プレイ時間の取得を利用できません。プラグインを再読み込みして再試行してください。")
                : plugin.RequestPlayTime()
                ? T("Requested ptime", "ptimeを照会しました")
                : T("Could not request ptime (wait 10 seconds between requests)", "ptimeを照会できませんでした（連続実行は10秒待機）");
        if (!string.IsNullOrWhiteSpace(statusMessage))
        {
            ImGui.SameLine();
            ImGui.TextDisabled(statusMessage);
        }
        ImGui.Separator();
        if (plugin.CaptureStopped)
        {
            ImGui.TextWrapped(T("Character capture stopped after three failures. Press Refresh Current Character to retry.",
                "キャラクター取得が3回失敗したため停止しました。「現在のキャラクターを再取得」で再開できます。"));
            ImGui.TextUnformatted(plugin.CaptureError ?? string.Empty);
        }
        if (plugin.RecoveryBlocked)
            ImGui.TextWrapped(T("Configuration could not be recovered. Restore a valid file and reload the plugin.",
                "設定を復元できませんでした。正常なファイルを戻してプラグインを再読み込みしてください。"));
        else if (plugin.SaveStopped)
            ImGui.TextWrapped(T("Saving stopped after three failures. Open Settings and retry saving.",
                "保存が3回失敗したため停止しました。設定から保存を再試行してください。"));
    }

    private void DrawOverview(IReadOnlyList<CharacterRecord> records)
    {
        if (records.Count == 0)
        {
            DisabledWrapped(T(
                "Log in with a character to add information here.",
                "キャラクターでログインすると、ここに情報が追加されます。"));
            return;
        }

        var pin = plugin.Configuration.PinCurrentCharacter;
        if (ImGui.Checkbox(T("Pin logged-in character", "ログイン中のキャラクターを先頭に表示"), ref pin))
        {
            plugin.Configuration.PinCurrentCharacter = pin;
            plugin.Repository.SaveSettings();
        }
        ulong pinned = pin && Plugin.ClientState.IsLoggedIn && Plugin.PlayerState.IsLoaded ? Plugin.PlayerState.ContentId : 0;
        var hide = plugin.Configuration.HideCompletedMainScenario;
        if (ImGui.Checkbox(T("Hide completed characters", "最新メインクエスト完了キャラを非表示"), ref hide))
        {
            plugin.Configuration.HideCompletedMainScenario = hide;
            plugin.Repository.SaveSettings();
        }
        const ImGuiTableFlags flags = ImGuiTableFlags.Sortable | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg |
                                      ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX;
        if (!ImGui.BeginTable("OverviewTable", 14, flags, new Vector2(0, -1)))
            return;

        ImGui.TableSetupColumn(T("Face", "顔"), ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 48 * ImGuiHelpers.GlobalScale + ImGui.CalcTextSize(T("Switch", "切替")).X + 2 * ImGui.GetStyle().FramePadding.X + ImGui.GetStyle().ItemSpacing.X);
        ImGui.TableSetupColumn(T("Character Name", "キャラクター名"), ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthStretch, 1.5f);
        ImGui.TableSetupColumn(T("Home World", "ホームワールド"), ImGuiTableColumnFlags.WidthFixed, 112 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(T("Lodestone ID", "ロドストID"), ImGuiTableColumnFlags.WidthFixed, 90 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(T("Current World", "現在地ワールド"), ImGuiTableColumnFlags.WidthFixed, 112 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(T("Current Job", "カレントジョブ"), ImGuiTableColumnFlags.WidthFixed, 150 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Lv", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 42 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(T("Playtime", "プレイ時間"), ImGuiTableColumnFlags.WidthFixed, 125 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(T("Main Scenario", "メインクエスト"), ImGuiTableColumnFlags.WidthFixed, 240 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(T("Expansion", "拡張"), ImGuiTableColumnFlags.WidthFixed, 150 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(T("MSQ total", "メイン全体"), ImGuiTableColumnFlags.WidthFixed, 85 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(T("Expansion total", "拡張全体"), ImGuiTableColumnFlags.WidthFixed, 85 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(T("Current patch", "進行パッチ"), ImGuiTableColumnFlags.WidthFixed, 110 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(T("Last Detected", "最終検出"), ImGuiTableColumnFlags.WidthFixed, 150 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        var specs = ImGui.TableGetSortSpecs();
        int column = !specs.IsNull && specs.SpecsCount != 0 ? specs.Specs.ColumnIndex : 1;
        bool descending = !specs.IsNull && specs.SpecsCount != 0 && specs.Specs.SortDirection == ImGuiSortDirection.Descending;
        if (sortedPin != pinned || sortedRevision != snapshotRevision || sortedColumn != column || sortedDescending != descending)
        {
            sortedOverview = records.ToArray();
            sortedPin = pinned;
            Array.Sort(sortedOverview, (a, b) =>
            {
                int priority = (pinned != 0 && b.ContentId == pinned).CompareTo(pinned != 0 && a.ContentId == pinned);
                return priority != 0 ? priority : CompareOverview(a, b, column, descending);
            });
            sortedRevision = snapshotRevision;
            sortedColumn = column;
            sortedDescending = descending;
        }
        if (!specs.IsNull) specs.SpecsDirty = false;
        bool loggedIn = Plugin.ClientState.IsLoggedIn && Plugin.PlayerState.IsLoaded;
        ulong currentId = loggedIn ? Plugin.PlayerState.ContentId : 0;
        foreach (var record in sortedOverview)
        {
            if (plugin.Configuration.HiddenCharacterIds.Contains(record.ContentId)) continue;
            if (hide && record.MainScenario?.Status == MainScenarioStatus.UpToDate) continue;
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.BeginDisabled(currentId == 0 || currentId == record.ContentId || Environment.TickCount64 < nextRelogClick);
            if (ImGui.SmallButton($"{T("Switch", "切替")}##relog{record.ContentId}"))
            {
                nextRelogClick = Environment.TickCount64 + 5000;
                var target = $"{record.CharacterName}@{record.HomeWorld}";
                if (!target.Any(char.IsControl) && !string.IsNullOrWhiteSpace(record.CharacterName) && !string.IsNullOrWhiteSpace(record.HomeWorld))
                {
                    statusMessage = Plugin.CommandManager.ProcessCommand($"/autoretainer relog {target}")
                        ? $"AutoRetainer → {target}" : T("Enable AutoRetainer to switch.", "切替にはAutoRetainerを有効にしてください。");
                }
            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            DrawFaceIcon(record);
            Cell(record.CharacterName);
            Cell(record.HomeWorld);
            Cell(record.LodestoneId ?? string.Empty);
            Cell(record.CurrentWorld);
            Cell(record.ClassJob);
            Cell(record.Level.ToString());
            Cell(PlayTimeFormatter.Format(record.PlayTimeMinutes, plugin.Configuration.PlayTimeDisplayMode, plugin.Localizer.Current));
            DrawAlternatePlaytime(record.PlayTimeMinutes);
            Cell(MainScenarioSummary(record.MainScenario));
            Cell(record.MainScenario?.ExpansionProgress?.Name ?? T("Not acquired", "未取得"));
            ProgressCell(FormatCompletion(record.MainScenario?.ExpansionProgress?.AllCampaigns), record.MainScenario?.ExpansionProgress?.AllCampaigns);
            ProgressCell(FormatCompletion(record.MainScenario?.ExpansionProgress?.Overall), record.MainScenario?.ExpansionProgress?.Overall);
            ProgressCell(FormatPatch(record.MainScenario?.ExpansionProgress), record.MainScenario?.ExpansionProgress?.PatchProgress);
            Cell(record.LastSeenAt.ToString("yyyy/MM/dd HH:mm:ss"));
        }

        ImGui.EndTable();
    }

    internal static int CompareOverview(CharacterRecord a, CharacterRecord b, int column, bool descending)
    {
        var x = descending ? b : a;
        var y = descending ? a : b;
        var names = StringComparer.OrdinalIgnoreCase;
        var m = x.MainScenario?.ExpansionProgress;
        var n = y.MainScenario?.ExpansionProgress;
        static ulong? Id(string? s) => ulong.TryParse(s, out var id) ? id : null;
        int result = column switch
        {
            0 => x.ProfileImageIsFace.CompareTo(y.ProfileImageIsFace),
            2 => names.Compare(x.HomeWorld, y.HomeWorld),
            3 => Nullable.Compare(Id(x.LodestoneId), Id(y.LodestoneId)),
            4 => names.Compare(x.CurrentWorld, y.CurrentWorld),
            5 => names.Compare(x.ClassJob, y.ClassJob),
            6 => x.Level.CompareTo(y.Level),
            7 => Nullable.Compare(x.PlayTimeMinutes, y.PlayTimeMinutes),
            8 => names.Compare(x.MainScenario?.QuestName, y.MainScenario?.QuestName),
            9 => Nullable.Compare(m?.Id, n?.Id),
            10 => Nullable.Compare(m?.AllCampaigns?.Percent, n?.AllCampaigns?.Percent),
            11 => Nullable.Compare(m?.Overall?.Percent, n?.Overall?.Percent),
            12 => names.Compare(m?.Patch, n?.Patch),
            13 => x.LastSeenAt.CompareTo(y.LastSeenAt),
            _ => names.Compare(x.CharacterName, y.CharacterName),
        };
        if (result == 0 && column == 12) result = Nullable.Compare(m?.PatchProgress?.Percent, n?.PatchProgress?.Percent);
        return result != 0 ? result : a.ContentId.CompareTo(b.ContentId);
    }

    private void DrawDetails(IReadOnlyList<CharacterRecord> records)
    {
        if (records.Count == 0)
        {
            DisabledWrapped(T("No information is available.", "表示できる情報がありません。"));
            return;
        }

        foreach (var record in records)
        {
            if (plugin.Configuration.HiddenCharacterIds.Contains(record.ContentId)) continue;
            if (!ImGui.CollapsingHeader($"{record.CharacterName} @ {record.HomeWorld}##{record.ContentId}",
                    ImGuiTreeNodeFlags.DefaultOpen))
                continue;

            if (!ImGui.BeginTabBar($"CharacterTabs{record.ContentId}"))
                continue;

            if (ImGui.BeginTabItem(T("Basic Information", "基本情報")))
            {
                if (ImGui.BeginTable($"Details{record.ContentId}", 2, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp))
                {
                    Detail("Content ID", $"{record.ContentId} (0x{record.ContentId:X16})");
                    Detail(T("Race / Clan / Sex", "種族 / 部族 / 性別"), $"{record.Race} / {record.Tribe} / {record.Sex}");
                    Detail(T("Current Job / Level", "カレントジョブ / レベル"), $"{record.ClassJob} / {record.Level}");
                    Detail(T("Playtime", "プレイ時間"), PlayTimeFormatter.Format(record.PlayTimeMinutes, plugin.Configuration.PlayTimeDisplayMode, plugin.Localizer.Current));
                    DrawAlternatePlaytime(record.PlayTimeMinutes);
                    Detail(T("Main Scenario", "メインクエスト"), MainScenarioSummary(record.MainScenario));
                    Detail(T("Expansion", "拡張"), record.MainScenario?.ExpansionProgress?.Name ?? T("Not acquired", "未取得"));
                    ProgressDetail(T("MSQ total", "メイン全体"), FormatCompletion(record.MainScenario?.ExpansionProgress?.AllCampaigns, true), record.MainScenario?.ExpansionProgress?.AllCampaigns);
                    ProgressDetail(T("Expansion total", "拡張全体"), FormatCompletion(record.MainScenario?.ExpansionProgress?.Overall, true), record.MainScenario?.ExpansionProgress?.Overall);
                    ProgressDetail(T("Current patch", "進行パッチ"), FormatPatch(record.MainScenario?.ExpansionProgress, true), record.MainScenario?.ExpansionProgress?.PatchProgress);
                    Detail(T("Grand Company", "グランドカンパニー"), EmptyAsDash(record.GrandCompany));
                    Detail(T("Guardian Deity", "守護神"), EmptyAsDash(record.GuardianDeity));
                    Detail(T("Starting City", "開始都市"), EmptyAsDash(record.StartTown));
                    Detail(T("First Detected", "初回検出"), record.FirstSeenAt.ToString("yyyy/MM/dd HH:mm:ss"));
                    Detail(T("Last Detected", "最終検出"), record.LastSeenAt.ToString("yyyy/MM/dd HH:mm:ss"));
                    ImGui.EndTable();
                }
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(T("Jobs", "ジョブ")))
            {
                DrawJobs(record);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem($"{T("Main Scenario", "メインクエスト")}###MainScenario"))
            {
                DrawMainScenario(record);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawMapping(IReadOnlyList<CharacterRecord> records)
    {
        ImGui.TextWrapped(T(
            "Maps character names and servers to Lodestone IDs and configuration folders. You can enter a Lodestone ID manually if automatic lookup fails.",
            "キャラクター名・サーバー名・Lodestone ID・設定フォルダ名の対応です。Lodestone IDは自動取得できない場合、直接入力できます。"));
        if (ImGui.Button(T("Batch Lookup Missing IDs", "未取得をまとめて検索")))
            _ = plugin.LookupAllLodestoneIdsAsync();
        ImGui.SameLine();
        ImGui.TextDisabled(batchLookupStatus);
        ImGui.Spacing();

        const ImGuiTableFlags flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg |
                                      ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY;
        if (ImGui.BeginTable("MappingTable", 6, flags, new Vector2(0, -70 * ImGuiHelpers.GlobalScale)))
        {
            ImGui.TableSetupColumn(T("Character Name", "キャラクター名"));
            ImGui.TableSetupColumn(T("Server", "サーバー名"));
            ImGui.TableSetupColumn(T("Lodestone ID", "ロドストID"), ImGuiTableColumnFlags.WidthFixed, 145 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn(T("Configuration Folder", "設定ファイル名"));
            ImGui.TableSetupColumn(T("Status", "状態"), ImGuiTableColumnFlags.WidthFixed, 80 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn(T("Action", "操作"), ImGuiTableColumnFlags.WidthFixed, 105 * ImGuiHelpers.GlobalScale);
            ImGui.TableHeadersRow();

            foreach (var record in records)
            {
                ImGui.PushID($"CID{record.ContentId:X16}");
                ImGui.TableNextRow();
                Cell(record.CharacterName);
                Cell(record.HomeWorld);

                ImGui.TableNextColumn();
                var lodestoneId = record.LodestoneId ?? string.Empty;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputText("##LodestoneId", ref lodestoneId, 32))
                    plugin.Repository.SetLodestoneId(record.ContentId, lodestoneId);

                Cell(record.ConfigFolderName);
                Cell(record.ConfigFolderExists ? T("Found", "確認済み") : T("Not found", "未確認"));

                ImGui.TableNextColumn();
                if (ImGui.Button(T("Lookup", "自動取得")))
                {
                    lookupResults[record.ContentId] = T("Looking up...", "取得中...");
                    _ = plugin.LookupLodestoneIdAsync(record);
                }
                if (lookupResults.TryGetValue(record.ContentId, out var result))
                    ImGui.TextDisabled(result);
                ImGui.PopID();
            }

            ImGui.EndTable();
        }

        if (ImGui.Button(T("Export CSV As...", "CSVを名前を付けて出力")))
        {
            var defaultName = $"character_file_mapping_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            fileDialogManager.SaveFileDialog(
                T("Choose where to save the CSV", "CSVの保存先を選択"),
                T("CSV file{.csv}", "CSVファイル{.csv}"),
                defaultName,
                ".csv",
                (success, path) => ExportCsv(plugin.Repository.Snapshot(), success, path),
                plugin.Configuration.CsvExportDirectory,
                true);
        }
        ImGui.SameLine();
        DisabledWrapped(T(
            "(Copies the file path to the clipboard after export)",
            "（出力後、ファイルパスをクリップボードへコピー）"));
    }

    private void DrawSettings()
    {
        if (ImGui.CollapsingHeader(T("Visible characters", "表示するキャラクター")))
        {
            foreach (var record in snapshot)
            {
                var visible = !plugin.Configuration.HiddenCharacterIds.Contains(record.ContentId);
                if (!ImGui.Checkbox($"{record.CharacterName}@{record.HomeWorld}###visibility{record.ContentId}", ref visible)) continue;
                if (visible) plugin.Configuration.HiddenCharacterIds.Remove(record.ContentId);
                else plugin.Configuration.HiddenCharacterIds.Add(record.ContentId);
                plugin.Repository.SaveSettings();
            }
        }
        if (!plugin.PlayTimeAvailable)
            ImGui.TextWrapped(T("Playtime acquisition is unavailable. Reload the plugin to retry.",
                "プレイ時間の取得を利用できません。プラグインを再読み込みして再試行してください。"));
        ImGui.TextWrapped(T("Changes are saved every 60 seconds and on logout or normal shutdown.",
            "変更は60秒ごとと、ログアウト・正常終了時に保存されます。"));
        if (plugin.ConfigurationRecovered)
            ImGui.TextWrapped(T("Configuration restored from a valid backup.", "正常なバックアップから設定を復元しました。"));
        if (plugin.SaveError is { } saveError)
        {
            ImGui.TextUnformatted(saveError);
            if (!plugin.RecoveryBlocked && ImGui.Button(T("Retry saving", "保存を再試行"))) plugin.RetrySave();
        }
        var autoPlayTime = plugin.Configuration.AutoRequestPlayTimeOnLogin;
        if (ImGui.Checkbox(T("Automatically acquire ptime on login", "ログイン時にptimeを自動取得する"), ref autoPlayTime))
            plugin.SetAutoRequestPlayTimeOnLogin(autoPlayTime);
        DisabledWrapped(T(
            "When enabled, runs the standard /playtime command once after login (default: OFF).",
            "有効時のみ、ログイン後にゲーム標準の /playtime を1回実行します（既定: OFF）。"));

        ImGui.Spacing();
        ImGui.Text(T("Playtime display format", "プレイ時間の表示形式"));
        var playTimeMode = plugin.Configuration.PlayTimeDisplayMode;
        if (ImGui.RadioButton(T("Game style (e.g. 12d 3h 45m)", "元の表示（例: 12日 3時間 45分）"), playTimeMode == PlayTimeDisplayMode.GameStyle))
        {
            plugin.Configuration.PlayTimeDisplayMode = PlayTimeDisplayMode.GameStyle;
            plugin.Repository.SaveSettings();
        }
        if (ImGui.RadioButton(T("Total hours (e.g. 00291:45:00)", "時間表示（例: 00291:45:00）"), playTimeMode == PlayTimeDisplayMode.TotalHours))
        {
            plugin.Configuration.PlayTimeDisplayMode = PlayTimeDisplayMode.TotalHours;
            plugin.Repository.SaveSettings();
        }
        DisabledWrapped(T(
            "Manual ptime requests are also limited to one every 10 seconds.",
            "手動のptime再取得も10秒以内の連続実行を抑止します。"));

        var commandDelay = plugin.Configuration.PlayTimeCommandDelaySeconds;
        ImGui.SetNextItemWidth(120 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputInt(T("Delay after commands become available", "コマンド使用可能後の待機秒数"), ref commandDelay))
        {
            plugin.Configuration.PlayTimeCommandDelaySeconds = Math.Clamp(commandDelay, 1, 60);
            plugin.Repository.SaveSettings();
        }
        DisabledWrapped(T(
            "1-60 seconds. Waits this long after the game allows text commands (default: 1 second).",
            "1～60秒。ゲーム側の禁止解除を検出してから、この秒数だけ待って実行します（デフォルト: 1秒）。"));

        ImGui.Spacing();
        var autoLookup = plugin.Configuration.AutoLookupLodestoneId;
        if (ImGui.Checkbox(T("Automatically look up Lodestone ID on login", "ログイン時にLodestone IDを自動取得する"), ref autoLookup))
        {
            plugin.Configuration.AutoLookupLodestoneId = autoLookup;
            plugin.Repository.SaveSettings();
        }
        DisabledWrapped(T(
            "When enabled, sends the character name and home world to the official Lodestone search.",
            "有効にすると、キャラクター名とホームワールドを公式Lodestoneへ送信して検索します。"));

        ImGui.Spacing();
        ImGui.Text(T("CSV output folder", "CSV出力先"));
        var exportDirectory = plugin.Configuration.CsvExportDirectory;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText("##CsvExportDirectory", ref exportDirectory, 1024))
        {
            plugin.Configuration.CsvExportDirectory = exportDirectory;
            plugin.Repository.SaveSettings();
        }

        if (ImGui.Button(T("Restore Default Folder", "既定の出力先に戻す")))
        {
            plugin.Configuration.CsvExportDirectory = Path.Combine(Plugin.PluginInterface.GetPluginConfigDirectory(), "exports");
            plugin.Repository.SaveSettings();
        }
        ImGui.SameLine();
        if (ImGui.Button(T("Open Output Folder", "出力先を開く")))
        {
            try
            {
                Directory.CreateDirectory(plugin.Configuration.CsvExportDirectory);
                Process.Start(new ProcessStartInfo
                {
                    FileName = plugin.Configuration.CsvExportDirectory,
                    UseShellExecute = true,
                });
            }
            catch (Exception exception)
            {
                Plugin.Log.Warning(exception, T("Could not open the CSV output folder.", "CSV出力先を開けませんでした"));
                statusMessage = T("Could not open the CSV output folder.", "CSV出力先を開けませんでした。");
            }
        }
    }

    private void DrawSupport()
    {
        StandardPluginTabs.DrawSupportContent(
            T("Support Roxyz0501's Development", "Roxyz0501の開発を支援"),
            T(
                "If this plugin is useful, you can optionally support development through Ko-fi. All features remain available without support.",
                "このプラグインが役に立った場合、Ko-fiから任意で開発を支援できます。支援の有無で機能が変わることはありません。"),
            T("Recipient: Roxyz0501", "受取人: Roxyz0501"),
            T("Support Roxyz0501 on Ko-fi", "Ko-fiでRoxyz0501を支援"));
    }

    private void DrawLanguageSettings()
    {
        var selected = plugin.Configuration.DisplayLanguage ?? DisplayLanguage.English;
        var changedCode = StandardPluginTabs.DrawLanguageSelector(
            T("Display language", "表示言語"),
            T(
            "The language is selected from game/Dalamud settings only on first launch, then remains fixed here.",
            "初回起動時のみゲーム／Dalamud設定から選択し、その後はここで選んだ言語に固定されます。"),
            ToLanguageCode(selected));
        if (changedCode is null)
            return;

        plugin.Configuration.DisplayLanguage = FromLanguageCode(changedCode);
        plugin.Repository.SaveSettings();
        statusMessage = string.Empty;
        batchLookupStatus = string.Empty;
        lookupResults.Clear();
    }

    private void DrawAlternatePlaytime(uint? minutes)
    {
        if (minutes is null || !ImGui.IsItemHovered()) return;
        var alternate = plugin.Configuration.PlayTimeDisplayMode == PlayTimeDisplayMode.TotalHours
            ? PlayTimeDisplayMode.GameStyle : PlayTimeDisplayMode.TotalHours;
        ImGui.SetTooltip(PlayTimeFormatter.Format(minutes, alternate, plugin.Localizer.Current));
    }

    private static void ProgressDetail(string label, string value, QuestCompletion? progress)
    {
        ImGui.TableNextRow();
        Cell(label);
        ProgressCell(value, progress);
    }

    private static void ProgressCell(string value, QuestCompletion? progress)
    {
        if (!ImGui.TableNextColumn()) return;
        if (progress is null) { ImGui.TextUnformatted(value); return; }
        var start = ImGui.GetCursorScreenPos();
        var size = new Vector2(Math.Max(1f, ImGui.GetContentRegionAvail().X), ImGui.GetTextLineHeight());
        float fraction = Math.Clamp((float)progress.Percent / 100f, 0f, 1f);
        var draw = ImGui.GetWindowDrawList();
        draw.AddRectFilled(start, start + size, 0xff313131);
        if (fraction > 0)
        {
            var tint = Vector4.Lerp(new Vector4(.46f, .24f, .10f, 1), new Vector4(.08f, .40f, .28f, 1), fraction);
            draw.AddRectFilled(start, start + new Vector2(size.X * fraction, size.Y), ImGui.ColorConvertFloat4ToU32(tint));
        }
        ImGui.TextColored(Vector4.One, value);
    }

    private static void Cell(string value)
    {
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(EmptyAsDash(value));
    }

    private static void DisabledWrapped(string value)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);
        ImGui.TextWrapped(value);
        ImGui.PopStyleColor();
    }

    private static void Detail(string label, string value)
    {
        ImGui.TableNextRow();
        Cell(label);
        ImGui.TableNextColumn();
        ImGui.TextWrapped(EmptyAsDash(value));
    }

    private static string EmptyAsDash(string value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    private void DrawFaceIcon(CharacterRecord record)
    {
        if (!record.ProfileImageIsFace || string.IsNullOrWhiteSpace(record.ProfileImagePath) || failedImages.Contains(record.ProfileImagePath))
        {
            ImGui.Dummy(new Vector2(42, 42) * ImGuiHelpers.GlobalScale);
            return;
        }

        Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? texture;
        try { texture = Plugin.TextureProvider.GetFromFile(record.ProfileImagePath).GetWrapOrDefault(); }
        catch
        {
            failedImages.Add(record.ProfileImagePath);
            ImGui.Dummy(new Vector2(42, 42) * ImGuiHelpers.GlobalScale);
            return;
        }
        if (texture is null)
            ImGui.Dummy(new Vector2(42, 42) * ImGuiHelpers.GlobalScale);
        else
            ImGui.Image(texture.Handle, new Vector2(42, 42) * ImGuiHelpers.GlobalScale);
    }

    private string MainScenarioSummary(MainScenarioProgress? progress) => progress is null
        ? T("Not acquired", "未取得")
        : $"{MainScenarioState(progress.Status)}: {progress.QuestName}";

    private string MainScenarioState(MainScenarioStatus status) => status switch
    {
        MainScenarioStatus.InProgress => T("In progress", "受注中"),
        MainScenarioStatus.Available => T("Not accepted", "未受注"),
        MainScenarioStatus.LastCompleted => T("Last completed", "直前にクリア"),
        MainScenarioStatus.UpToDate => T("Latest available story completed", "現在の最終クエストまで完了"),
        _ => T("Not acquired", "未取得"),
    };

    private void DrawMainScenario(CharacterRecord record)
    {
        if (record.MainScenario is not { } progress)
            ImGui.TextWrapped(T("Not acquired", "未取得"));
        else if (ImGui.BeginTable($"MainScenario{record.ContentId}", 2, ImGuiTableFlags.BordersInnerH))
        {
            Detail(T("Status", "状態"), MainScenarioState(progress.Status));
            Detail(T("Quest", "クエスト"), progress.QuestName);
            Detail(T("Chapter", "章"), progress.Chapter);
            Detail(T("Expansion", "拡張"), progress.ExpansionProgress?.Name ?? T("Not acquired", "未取得"));
            ProgressDetail(T("MSQ total", "メイン全体"), FormatCompletion(progress.ExpansionProgress?.AllCampaigns, true), progress.ExpansionProgress?.AllCampaigns);
            ProgressDetail(T("Expansion total", "拡張全体"), FormatCompletion(progress.ExpansionProgress?.Overall, true), progress.ExpansionProgress?.Overall);
            ProgressDetail(T("Current patch", "進行パッチ"), FormatPatch(progress.ExpansionProgress, true), progress.ExpansionProgress?.PatchProgress);
            Detail(T("Quest ID", "クエストID"), progress.QuestId.ToString());
            Detail(T("Quest sequence", "クエスト内部進行値"), progress.Sequence?.ToString() ?? "—");
            Detail(T("Last Detected", "最終検出"), progress.ObservedAt.ToString("yyyy/MM/dd HH:mm:ss"));
            ImGui.EndTable();
        }
        ImGui.TextWrapped(T("Reads received game data every 10 seconds while logged in. Offline characters show the last saved observation. Quest sequence is not a percentage.",
            "ログイン中は受信済みのゲーム情報を10秒ごとに読み取ります。未ログインのキャラクターは最後の記録を表示します。内部進行値は進捗率ではありません。"));
        ImGui.TextWrapped(T("Patch progress counts required quests within the displayed patch.",
            "進行パッチの割合は、表示されたパッチ内の必要クエストを対象にします。"));
    }

    private string FormatPatch(MainScenarioExpansion? scope, bool counts = false) =>
        scope?.Patch is { } patch ? $"{patch} · {FormatCompletion(scope.PatchProgress, counts)}" : T("Not acquired", "未取得");

    private string FormatCompletion(QuestCompletion? progress, bool includeCounts = false)
    {
        if (progress is null) return T("Not acquired", "未取得");
        var percent = progress.Percent.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        return includeCounts && !progress.MilestoneOnly ? $"{percent}% ({progress.Completed}/{progress.Total})" : $"{percent}%";
    }

    private void DrawJobs(CharacterRecord record)
    {
        if (record.Jobs.Count == 0)
        {
            DisabledWrapped(T(
                "Job information updates automatically after it is loaded into memory.",
                "ジョブ情報はメモリへ読み込まれた後に自動更新されます。"));
            return;
        }

        if (!ImGui.BeginTable($"Jobs{record.ContentId}", 3,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
                new Vector2(0, 250 * ImGuiHelpers.GlobalScale)))
            return;

        ImGui.TableSetupColumn(T("Job Name", "ジョブ名"), ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn(T("Abbreviation", "略称"), ImGuiTableColumnFlags.WidthFixed, 90 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(T("Level", "レベル"), ImGuiTableColumnFlags.WidthFixed, 75 * ImGuiHelpers.GlobalScale);
        ImGui.TableHeadersRow();
        foreach (var job in record.Jobs.OrderBy(job => job.RowId))
        {
            ImGui.TableNextRow();
            Cell(job.RowId == record.CurrentClassJobId
                ? F("{0} (Current)", "{0}（カレント）", job.Name)
                : job.Name);
            Cell(job.Abbreviation);
            Cell(job.Level > 0 ? job.Level.ToString() : T("Locked", "未開放"));
        }
        ImGui.EndTable();
    }

    private void ExportCsv(IReadOnlyList<CharacterRecord> records, bool success, string path)
    {
        if (!success)
            return;

        try
        {
            CsvExporter.ExportToPath(records, path, plugin.Localizer.Current);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                plugin.Configuration.CsvExportDirectory = directory;
                plugin.Repository.SaveSettings();
            }
            statusMessage = F("CSV export completed: {0}", "CSV出力完了: {0}", path);
            ImGui.SetClipboardText(path);
        }
        catch (Exception exception)
        {
            Plugin.Log.Error(exception, T("CSV export failed.", "CSV出力に失敗しました"));
            statusMessage = T(
                "CSV export failed. Check the destination folder.",
                "CSV出力に失敗しました。保存先を確認してください。");
        }
    }

    private string T(string english, string japanese) => plugin.Localizer.Text(english, japanese);

    private string F(string english, string japanese, params object[] arguments) =>
        plugin.Localizer.Format(english, japanese, arguments);

    private static string ToLanguageCode(DisplayLanguage language) => language switch
    {
        DisplayLanguage.Japanese => "ja",
        DisplayLanguage.German => "de",
        DisplayLanguage.French => "fr",
        DisplayLanguage.Korean => "ko",
        DisplayLanguage.SimplifiedChinese => "zh-Hans",
        DisplayLanguage.TraditionalChinese => "zh-Hant",
        _ => "en",
    };

    private static DisplayLanguage FromLanguageCode(string code) => code switch
    {
        "ja" => DisplayLanguage.Japanese,
        "de" => DisplayLanguage.German,
        "fr" => DisplayLanguage.French,
        "ko" => DisplayLanguage.Korean,
        "zh-Hans" => DisplayLanguage.SimplifiedChinese,
        "zh-Hant" => DisplayLanguage.TraditionalChinese,
        _ => DisplayLanguage.English,
    };
}
