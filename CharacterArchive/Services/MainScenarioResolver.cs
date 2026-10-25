using CharacterArchive.Models;

namespace CharacterArchive.Services;

internal sealed record MainScenarioQuest(uint Id, string Name, string Chapter, uint[] PreviousIds);

/// <summary>Pure interpretation of received guide/quest state. Zero guide IDs never imply completion.</summary>
internal static class MainScenarioResolver
{
    public static uint[] FindTerminalIds(IEnumerable<MainScenarioQuest> quests)
    {
        var rows = quests.ToArray();
        var previous = rows.SelectMany(q => q.PreviousIds).ToHashSet();
        return rows.Where(q => !previous.Contains(q.Id)).Select(q => q.Id).ToArray();
    }

    public static MainScenarioProgress? Resolve(IReadOnlyList<ushort> guide, int selected,
        IReadOnlyDictionary<uint, MainScenarioQuest> quests, IReadOnlyList<uint> terminalIds,
        Func<uint, bool> completed, Func<uint, bool> accepted, Func<uint, byte> sequence, DateTime now)
    {
        // Only an unambiguous endpoint derived from the installed quest graph can prove latest completion.
        // This also prevents New Game+ guide entries from downgrading an already completed story.
        if (terminalIds.Count == 1 && quests.TryGetValue(terminalIds[0], out var end) && completed(end.Id))
            return new(end.Id, end.Name, end.Chapter, MainScenarioStatus.UpToDate, null, now);

        var order = Enumerable.Range(0, Math.Min(3, guide.Count))
            .OrderBy(i => i == selected ? 0 : 1);
        foreach (var i in order)
        {
            if (guide[i] == 0 || !quests.TryGetValue(0x10000u | guide[i], out var quest) || completed(quest.Id)) continue;
            var active = accepted(quest.Id);
            return new(quest.Id, quest.Name, quest.Chapter,
                active ? MainScenarioStatus.InProgress : MainScenarioStatus.Available,
                active ? sequence(quest.Id) : null, now);
        }
        if (guide.Count > 3 && guide[3] != 0 && quests.TryGetValue(0x10000u | guide[3], out var last) && completed(last.Id))
            return new(last.Id, last.Name, last.Chapter, MainScenarioStatus.LastCompleted, null, now);
        return null;
    }

    public static bool SameObservation(MainScenarioProgress? left, MainScenarioProgress right) =>
        left is not null && left with { ObservedAt = right.ObservedAt } == right;
}
