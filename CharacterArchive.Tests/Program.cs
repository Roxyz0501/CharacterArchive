using System.Text;
using CharacterArchive;
using CharacterArchive.Models;
using CharacterArchive.Services;

var failures = new List<string>();

AssertEqual(DisplayLanguage.Japanese, LanguageResolver.Initialize(null, "ja", false), "初回Dalamud日本語");
AssertEqual(DisplayLanguage.Japanese, LanguageResolver.Initialize(null, "en", true), "初回ゲーム日本語");
AssertEqual(DisplayLanguage.English, LanguageResolver.Initialize(null, null, false), "初回検出不能は英語fallback");
AssertEqual(DisplayLanguage.English, LanguageResolver.Initialize(DisplayLanguage.English, "ja", true), "保存済みEnglish維持");
AssertEqual(DisplayLanguage.Japanese, LanguageResolver.Initialize(DisplayLanguage.Japanese, "en", false), "保存済み日本語維持");
AssertEqual(DisplayLanguage.Japanese, LanguageResolver.Initialize((DisplayLanguage)0, "ja", false), "途中版Auto値は初回検出へ移行");

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

    var englishPath = Path.Combine(testDirectory, "english.csv");
    CsvExporter.ExportToPath([], englishPath, ResolvedLanguage.English);
    Assert(File.ReadAllText(englishPath, Encoding.UTF8)
        .StartsWith("Character Name,Server,Lodestone ID,Configuration Folder", StringComparison.Ordinal), "CSV英語列名");
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
