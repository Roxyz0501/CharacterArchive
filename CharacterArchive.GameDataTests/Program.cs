using CharacterArchive.Models;
using CharacterArchive.Services;
using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;

if (args.Length != 1) { Console.Error.WriteLine("Provide an installed game/sqpack directory for read-only catalogue checks."); return 2; }
uint[]? expected = null;
var checks = 0;
foreach (var language in new[] { Language.Japanese, Language.English, Language.German, Language.French })
{
    using var game = new GameData(args[0], new LuminaOptions { DefaultExcelLanguage = language, CacheFileResources = false });
    var quests = game.Excel.GetSheet<Quest>()
        .Where(q => q.RowId != 0 && q.JournalGenre.IsValid && q.JournalGenre.Value.Icon == 61412 && !string.IsNullOrWhiteSpace(q.Name.ToString()))
        .Select(q => new MainScenarioQuest(q.RowId, q.Name.ToString(), q.JournalGenre.Value.Name.ToString(),
            q.PreviousQuest.Select(p => p.RowId).Where(id => id != 0).ToArray())).ToDictionary(q => q.Id);
    var ends = MainScenarioResolver.FindTerminalIds(quests.Values);
    if (quests.Count < 500 || ends.Length != 1) throw new Exception("Installed MSQ graph is empty or ambiguous.");
    checks++;
    if (expected is not null && !expected.SequenceEqual(ends)) throw new Exception("MSQ endpoints differ across game languages.");
    checks++;
    expected = ends;
    var resolved = MainScenarioResolver.Resolve([0, 0, 0, 0], 0, quests, ends, id => ends.Contains(id), _ => false, _ => 0, DateTime.UnixEpoch);
    if (resolved is not { Status: MainScenarioStatus.UpToDate } || string.IsNullOrWhiteSpace(resolved.QuestName) || string.IsNullOrWhiteSpace(resolved.Chapter))
        throw new Exception("Real catalogue endpoint does not resolve with localized names.");
    checks++;
    Console.WriteLine($"{language}: MSQs={quests.Count}, terminal={ends[0]}, name={resolved.QuestName}");
}
Console.WriteLine($"Offline game catalogue checks passed: {checks}. No player data or game process was accessed.");
return 0;
