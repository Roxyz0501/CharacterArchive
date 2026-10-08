using System.Text;
using CharacterArchive;
using CharacterArchive.Models;
using CharacterArchive.Services;

var failures = new List<string>();

AssertEqual(DisplayLanguage.Japanese, LanguageResolver.Initialize(null, "Japanese", "en"), "ゲーム日本語優先");
AssertEqual(DisplayLanguage.German, LanguageResolver.Initialize(null, "de-DE", "ja"), "ゲームGerman優先");
AssertEqual(DisplayLanguage.French, LanguageResolver.Initialize(null, null, "fr-FR"), "Dalamud French fallback");
AssertEqual(DisplayLanguage.Korean, LanguageResolver.Initialize(null, null, "ko_KR"), "Dalamud Korean fallback");
AssertEqual(DisplayLanguage.SimplifiedChinese, LanguageResolver.Initialize(null, "zh-CN", "en"), "簡体字地域コード");
AssertEqual(DisplayLanguage.TraditionalChinese, LanguageResolver.Initialize(null, "zh-Hant", "en"), "繁体字明示コード");
AssertEqual(DisplayLanguage.English, LanguageResolver.Initialize(null, "zh", "unknown"), "曖昧zhはEnglish fallback");
AssertEqual(DisplayLanguage.English, LanguageResolver.Initialize(null, null, null), "検出不能はEnglish fallback");
AssertEqual(DisplayLanguage.English, LanguageResolver.Initialize(DisplayLanguage.English, "ja", "ja"), "保存済みEnglish維持");
AssertEqual(DisplayLanguage.Japanese, LanguageResolver.Initialize(DisplayLanguage.Japanese, "en", "en"), "保存済み日本語維持");
AssertEqual(DisplayLanguage.Korean, LanguageResolver.Initialize(DisplayLanguage.Korean, "en", "en"), "保存済み韓国語維持");
AssertEqual(DisplayLanguage.Japanese, LanguageResolver.Initialize((DisplayLanguage)0, "ja", "en"), "旧Auto値は再解決");
foreach (var savedLanguage in Enum.GetValues<DisplayLanguage>())
    AssertEqual(savedLanguage, LanguageResolver.Initialize(savedLanguage, "en", "ja"), $"保存済み言語維持:{savedLanguage}");
AssertEqual(1, (int)DisplayLanguage.English, "旧English enum番号維持");
AssertEqual(2, (int)DisplayLanguage.Japanese, "旧日本語enum番号維持");
AssertEqual(0, LocalizationCatalog.Validate().Count, "7言語リソースの空値・書式引数整合");

AssertEqual("FFXIV_CHR0123456789ABCDEF", CharacterFileLocator.GetFolderName(0x0123456789ABCDEF),
    "設定フォルダ名");

var profileHtml = """
    <meta property="og:image" content="https://lds-img.finalfantasyxiv.com/h/site.png">
    <div class="frame__chara__face">
      <img src="https://img2.finalfantasyxiv.com/f/character-face-fc0.jpg?123" width="40" height="40">
    </div>
    """;
AssertEqual(
    "https://img2.finalfantasyxiv.com/f/character-face-fc0.jpg?123",
    LodestoneLookupService.ExtractProfileImageUrl(profileHtml) ?? string.Empty,
    "Lodestone顔画像をOG画像より優先");

AssertEqual("12日 3時間 45分", PlayTimeFormatter.Format(17_505, PlayTimeDisplayMode.GameStyle), "ptime元表示");
AssertEqual("12d 3h 45m", PlayTimeFormatter.Format(17_505, PlayTimeDisplayMode.GameStyle, ResolvedLanguage.English), "ptime英語表示");
AssertEqual("12 T 3 Std. 45 Min.", PlayTimeFormatter.Format(17_505, PlayTimeDisplayMode.GameStyle, ResolvedLanguage.German), "ptimeドイツ語表示");
AssertEqual("12일 3시간 45분", PlayTimeFormatter.Format(17_505, PlayTimeDisplayMode.GameStyle, ResolvedLanguage.Korean), "ptime韓国語表示");
AssertEqual("12天 3小时 45分钟", PlayTimeFormatter.Format(17_505, PlayTimeDisplayMode.GameStyle, ResolvedLanguage.SimplifiedChinese), "ptime簡体字表示");
AssertEqual("00291:45:00", PlayTimeFormatter.Format(17_505, PlayTimeDisplayMode.TotalHours), "ptime時間表示");
AssertEqual("未取得", PlayTimeFormatter.Format(null, PlayTimeDisplayMode.TotalHours), "ptime未取得");

var testDirectory = Path.Combine(Path.GetTempPath(), "CharacterArchiveTests", Guid.NewGuid().ToString("N"));
try
{
    var output = CsvExporter.Export([
        new CharacterRecord
        {
            CharacterName = "Example, \"Character\"",
            HomeWorld = "ExampleWorld",
            LodestoneId = "TEST-ID",
            ConfigFolderName = "FFXIV_CHR0123456789ABCDEF",
        },
    ], testDirectory);

    var bytes = File.ReadAllBytes(output);
    Assert(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "CSVがUTF-8 BOM付き");
    var csv = File.ReadAllText(output, Encoding.UTF8);
    Assert(csv.StartsWith("キャラクター名,サーバー名,ロドストID,設定ファイル名", StringComparison.Ordinal), "CSV列順");
    Assert(csv.Contains("\"Example, \"\"Character\"\"\"", StringComparison.Ordinal), "CSVエスケープ");

    var chosenPath = Path.Combine(testDirectory, "任意の場所", "mapping.csv");
    CsvExporter.ExportToPath([], chosenPath);
    Assert(File.Exists(chosenPath), "任意パスへのCSV出力");

    foreach (var language in Enum.GetValues<ResolvedLanguage>())
    {
        var localizedPath = Path.Combine(testDirectory, $"schema-{language}.csv");
        CsvExporter.ExportToPath([], localizedPath, language);
        Assert(File.ReadAllText(localizedPath, Encoding.UTF8)
            .StartsWith("キャラクター名,サーバー名,ロドストID,設定ファイル名", StringComparison.Ordinal),
            $"CSVスキーマは言語非依存:{language}");
    }
}
finally
{
    if (Directory.Exists(testDirectory))
        Directory.Delete(testDirectory, recursive: true);
}

if (failures.Count > 0)
{
    foreach (var failure in failures)
        Console.Error.WriteLine($"FAIL: {failure}");
    return 1;
}

Console.WriteLine("All CharacterArchive core tests passed.");
return 0;

void Assert(bool condition, string name)
{
    if (!condition)
        failures.Add(name);
}

void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        failures.Add($"{name}: expected={expected}, actual={actual}");
}
