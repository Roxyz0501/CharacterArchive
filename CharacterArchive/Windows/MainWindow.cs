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

    public override void Draw()
    {
        fileDialogManager.Draw();
        var records = plugin.Repository.Snapshot();
        DrawHeader(records.Count);

        if (!ImGui.BeginTabBar("CharacterArchiveTabs"))
            return;

        if (ImGui.BeginTabItem(T("Character Overview", "キャラクター概要")))
        {
            DrawOverview(records);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem(T("Character Details", "キャラクター詳細")))
        {
            DrawDetails(records);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem(T("Character File Mapping", "キャラファイル紐づけ")))
        {
            DrawMapping(records);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem(T("Settings", "設定")))
        {
            DrawSettings();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem($"{T("Language settings", "言語設定")}###LanguageSettings"))
        {
            DrawLanguageSettings();
            ImGui.EndTabItem();
        }

        StandardPluginTabs.PushSupportTabStyle();
        var supportTabOpen = ImGui.BeginTabItem($"{T("Support", "支援")}###Support");
        StandardPluginTabs.PopSupportTabStyle();
        if (supportTabOpen)
        {
            DrawSupport();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    internal void SetLookupResult(ulong contentId, string message) => lookupResults[contentId] = message;

    internal void SetBatchLookupStatus(string message) => batchLookupStatus = message;

    private void DrawHeader(int count)
    {
        ImGui.Text(F("Saved characters: {0}", "保存済みキャラクター: {0}", count));
        ImGui.SameLine();
        if (ImGui.Button(T("Refresh Current Character", "現在のキャラクターを再取得")))
            plugin.CaptureCurrentCharacter();
        ImGui.SameLine();
        if (ImGui.Button(T("Refresh ptime", "ptime再取得")))
            statusMessage = plugin.RequestPlayTime()
                ? T("Requested ptime", "ptimeを照会しました")
                : T("Could not request ptime (wait 10 seconds between requests)", "ptimeを照会できませんでした（連続実行は10秒待機）");
        if (!string.IsNullOrWhiteSpace(statusMessage))
        {
            ImGui.SameLine();
            ImGui.TextDisabled(statusMessage);
        }
        ImGui.Separator();
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

        const ImGuiTableFlags flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg |
                                      ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY;
        if (!ImGui.BeginTable("OverviewTable", 9, flags, new Vector2(0, -1)))
            return;

        ImGui.TableSetupColumn(T("Face", "顔"), ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 48 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(T("Character Name", "キャラクター名"), ImGuiTableColumnFlags.WidthStretch, 1.5f);
        ImGui.TableSetupColumn(T("Home World", "ホームワールド"), ImGuiTableColumnFlags.WidthFixed, 112 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(T("Lodestone ID", "ロドストID"), ImGuiTableColumnFlags.WidthFixed, 90 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(T("Current World", "現在地ワールド"), ImGuiTableColumnFlags.WidthFixed, 112 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(T("Current Job", "カレントジョブ"), ImGuiTableColumnFlags.WidthFixed, 150 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Lv", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 42 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(T("Playtime", "プレイ時間"), ImGuiTableColumnFlags.WidthFixed, 125 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(T("Last Login Detected", "最終ログイン検出"), ImGuiTableColumnFlags.WidthFixed, 150 * ImGuiHelpers.GlobalScale);
        ImGui.TableHeadersRow();

        foreach (var record in records)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawFaceIcon(record);
            Cell(record.CharacterName);
            Cell(record.HomeWorld);
            Cell(record.LodestoneId ?? string.Empty);
            Cell(record.CurrentWorld);
            Cell(record.ClassJob);
            Cell(record.Level.ToString());
            Cell(PlayTimeFormatter.Format(record.PlayTimeMinutes, plugin.Configuration.PlayTimeDisplayMode, plugin.Localizer.Current));
            Cell(record.LastSeenAt.ToString("yyyy/MM/dd HH:mm:ss"));
        }

        ImGui.EndTable();
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
                ImGui.PushID(unchecked((int)record.ContentId));
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
                (success, path) => ExportCsv(records, success, path),
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
            plugin.Configuration.CsvExportDirectory = exportDirectory;
        if (ImGui.IsItemDeactivatedAfterEdit())
            plugin.Repository.SaveSettings();

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
        Cell(value);
    }

    private static string EmptyAsDash(string value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    private static void DrawFaceIcon(CharacterRecord record)
    {
        if (!record.ProfileImageIsFace || string.IsNullOrWhiteSpace(record.ProfileImagePath) || !File.Exists(record.ProfileImagePath))
        {
            ImGui.Dummy(new Vector2(42, 42) * ImGuiHelpers.GlobalScale);
            return;
        }

        var texture = Plugin.TextureProvider.GetFromFile(record.ProfileImagePath).GetWrapOrDefault();
        if (texture is null)
            ImGui.Dummy(new Vector2(42, 42) * ImGuiHelpers.GlobalScale);
        else
            ImGui.Image(texture.Handle, new Vector2(42, 42) * ImGuiHelpers.GlobalScale);
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
