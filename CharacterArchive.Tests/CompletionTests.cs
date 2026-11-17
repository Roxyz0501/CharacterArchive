using CharacterArchive;
using CharacterArchive.Models;
using CharacterArchive.Services;

internal static class CompletionTests
{
    public static void Run(List<string> failures)
    {
        var checks = 0;
        void Check(bool ok, string text) { checks++; if (!ok) failures.Add(text); }
        var quests = new[]
        {
            new MainScenarioQuest(65537, "City A", "Launch", [], 0, "Expansion A", 1),
            new MainScenarioQuest(65538, "City B", "Launch", [], 0, "Expansion A", 1),
            new MainScenarioQuest(65539, "Merge", "Launch", [65537, 65538], 0, "Expansion A", 1, 2),
            new MainScenarioQuest(65540, "Parallel A", "Launch", [65539], 0, "Expansion A", 1),
            new MainScenarioQuest(65541, "Parallel B", "Launch", [65539], 0, "Expansion A", 1),
            new MainScenarioQuest(65542, "Launch end", "Launch", [65540, 65541], 0, "Expansion A", 1),
            new MainScenarioQuest(65543, "Patch", "Post-launch", [65542], 0, "Expansion A", 2),
            new MainScenarioQuest(65544, "Patch end", "Post-launch", [65543], 0, "Expansion A", 2),
            new MainScenarioQuest(65545, "Next expansion", "Launch", [65544], 1, "Expansion B", 3),
        }.ToDictionary(q => q.Id);
        var current = new MainScenarioProgress(65540, "Parallel A", "Launch", MainScenarioStatus.InProgress, 5, DateTime.UnixEpoch);
        var calculator = new MainScenarioCompletionCalculator(quests);
        var reads = 0;
        var done = new HashSet<uint> { 65537, 65539, 65541 };
        bool Completed(uint id) { reads++; return done.Contains(id); }
        var result = calculator.Calculate(1, current, new byte[] { 1 }, Completed)!;
        Check(result.Id == 0 && result.Name == "Expansion A", "expansion is selected from current quest, not latest patch");
        Check(result.Overall == new QuestCompletion(3, 7) && result.Overall.Percent == 42.8m, "expansion count excludes alternate city and includes both AND branches plus patches");
        Check(result.BaseCampaign == new QuestCompletion(3, 5) && result.BaseCampaign.Percent == 60m, ".0 excludes patch chapters without excluding required parallel routes");
        var priorReads = reads;
        var same = calculator.Calculate(1, current, new byte[] { 1 }, Completed);
        Check(ReferenceEquals(result, same) && reads == priorReads, "unchanged flags/current quest cause zero completion API reads");
        Check(reads <= 8, "full and .0 share memoized completion reads within current expansion");
        done.Add(65540);
        var changed = calculator.Calculate(1, current, new byte[] { 2 }, Completed)!;
        Check(changed.Overall == new QuestCompletion(4, 7) && changed.BaseCampaign == new QuestCompletion(4, 5), "completion flag changes refresh percentages even if guide quest stays unchanged");
        done.Add(65542);
        var patch = calculator.Calculate(1, current with { QuestId = 65543 }, new byte[] { 3 }, Completed)!;
        Check(patch.Overall?.Percent == 71.4m && patch.BaseCampaign is { Percent: 100m, MilestoneOnly: true }, ".0 stays 100 while patch storyline continues");
        var cleared = calculator.Calculate(1, current with { QuestId = 65544 }, new byte[] { 4 }, id => id is 65542 or 65544)!;
        Check(cleared.Overall is { Percent: 100m, MilestoneOnly: true } && cleared.BaseCampaign?.Percent == 100m, "endpoint proof handles skip/reworked flags without inventing counts");
        var otherCharacter = calculator.Calculate(2, current, new byte[] { 4 }, id => id is 65537 or 65539)!;
        Check(otherCharacter.Overall == new QuestCompletion(2, 7), "character switch invalidates identical flag cache key");
        var firstQuest = calculator.Calculate(3, current with { QuestId = 65538 }, new byte[] { 0 }, _ => false)!;
        Check(firstQuest.Overall == new QuestCompletion(0, 7), "current starting route is recognized before its first completion");
        var unknownRoute = calculator.Calculate(4, current with { QuestId = 65539 }, new byte[] { 0 }, _ => false)!;
        Check(unknownRoute.Overall is null && unknownRoute.BaseCampaign is null, "unresolved exclusive route is unavailable, never guessed as zero percent");
        var next = calculator.Calculate(4, current with { QuestId = 65545 }, new byte[] { 1 }, _ => false)!;
        Check(next.Id == 1 && next.Overall == new QuestCompletion(0, 1) && next.BaseCampaign == new QuestCompletion(0, 1), "next expansion starts at zero with separate denominator");
        Check(new QuestCompletion(9999, 10000).Percent == 99.9m, "unfinished progress cannot round to 100");
        Check(calculator.Calculate(1, current with { QuestId = 999 }, [], _ => false) is null, "unknown quest cannot provide expansion progress");

        var config = new Configuration { Characters = [new CharacterRecord { ContentId = 1, CharacterName = "Synthetic Character", HomeWorld = "ExampleWorld", MainScenario = current with { ExpansionProgress = changed } }] };
        var restored = ConfigurationCodec.Decode(ConfigurationCodec.Encode(config));
        Check(restored.Characters[0].MainScenario?.ExpansionProgress == changed, "expansion and quest counts survive config roundtrip");
        Check(ConfigurationCodec.Decode("{\"Version\":4,\"Characters\":[{\"ContentId\":1,\"CharacterName\":\"Synthetic\",\"HomeWorld\":\"ExampleWorld\",\"MainScenario\":{\"QuestId\":65540,\"QuestName\":\"Quest\",\"Chapter\":\"Chapter\",\"Status\":1,\"ObservedAt\":\"2026-01-01T00:00:00\"}}]}")
            .Characters[0].MainScenario?.ExpansionProgress is null, "old saved progress migrates without fabricated percentages");
        var repo = new CharacterRepository(() => { }, config);
        var revision = repo.Revision;
        repo.SetMainScenario(1, current with { ExpansionProgress = changed, ObservedAt = DateTime.UnixEpoch.AddSeconds(10) });
        Check(repo.Revision == revision, "unchanged nested progress causes no dirty mark or write");
        config.Characters[0].MainScenario = current with { ExpansionProgress = new(0, "Expansion A", new(6, 5), null) };
        try { ConfigurationCodec.Decode(ConfigurationCodec.Encode(config)); Check(false, "invalid ratio rejected"); }
        catch (InvalidDataException) { Check(true, "invalid ratio rejected"); }
        Console.WriteLine($"Expansion completion checks passed: {checks}; initial completion reads={priorReads}, unchanged=0");
    }
}
