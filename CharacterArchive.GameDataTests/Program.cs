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
            q.PreviousQuest.Select(p => p.RowId).Where(id => id != 0).ToArray(), q.Expansion.RowId,
            q.Expansion.Value.Name.ToString(), q.JournalGenre.RowId, q.PreviousQuestJoin)).ToDictionary(q => q.Id);
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
    foreach (var expansion in quests.Values.GroupBy(q => q.ExpansionId))
    {
        var all = expansion.ToArray();
        var overallEnd = MainScenarioResolver.FindTerminalIds(all);
        var firstGenre = all.Min(q => q.GenreId);
        var launchEnd = MainScenarioResolver.FindTerminalIds(all.Where(q => q.GenreId == firstGenre));
        if (overallEnd.Length != 1 || launchEnd.Length != 1 || string.IsNullOrWhiteSpace(all[0].ExpansionName))
            throw new Exception("Expansion/.0 graph boundary or localized expansion name unavailable.");
        checks++;
        var calculator = new MainScenarioCompletionCalculator(quests);
        var observation = new MainScenarioProgress(overallEnd[0], "Fixture", "Fixture", MainScenarioStatus.Available, null, DateTime.UnixEpoch);
        var reads = 0;
        bool Completed(uint id) { reads++; return id != overallEnd[0] && id != launchEnd[0]; }
        var value = calculator.Calculate(1, observation, new byte[] { 1 }, Completed)!;
        if (value.Overall is not { Total: > 0 } || value.BaseCampaign is not { Total: > 0 } ||
            value.Overall.Total < value.BaseCampaign.Total || value.Overall.Percent >= 100 || value.BaseCampaign.Percent >= 100 || reads > all.Length)
            throw new Exception("Expansion/.0 counts or bounded read count invalid.");
        checks++;
        var before = reads;
        if (calculator.Calculate(1, observation, new byte[] { 1 }, Completed) != value || reads != before)
            throw new Exception("Unchanged catalogue observation causes extra completion reads.");
        checks++;
        var complete = calculator.Calculate(1, observation, new byte[] { 2 }, _ => true)!;
        if (complete.Overall?.Percent != 100 || complete.BaseCampaign?.Percent != 100) throw new Exception("Cleared expansion must reach 100% in both scopes.");
        checks++;
        Console.WriteLine($"  Expansion {expansion.Key}: overall={value.Overall.Completed}/{value.Overall.Total}, .0={value.BaseCampaign.Completed}/{value.BaseCampaign.Total}, reads={before}, unchanged=0");
    }
}
Console.WriteLine($"Offline game catalogue checks passed: {checks}. No player data or game process was accessed.");
return 0;
