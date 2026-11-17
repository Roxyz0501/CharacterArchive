using CharacterArchive.Models;

namespace CharacterArchive.Services;

/// <summary>Quest-count progress on the player's required route, not time or internal quest sequence.</summary>
internal sealed class MainScenarioCompletionCalculator(IReadOnlyDictionary<uint, MainScenarioQuest> quests)
{
    private ulong cachedContentId;
    private uint cachedQuestId;
    private byte[]? cachedFlags;
    private MainScenarioExpansion? cached;
    private readonly Dictionary<(uint Expansion, bool Base), Dictionary<uint, MainScenarioQuest>> scopes = [];

    public MainScenarioExpansion? Calculate(ulong contentId, MainScenarioProgress progress,
        ReadOnlySpan<byte> completionFlags, Func<uint, bool> completed)
    {
        if (cachedFlags is not null && cachedContentId == contentId && cachedQuestId == progress.QuestId &&
            completionFlags.SequenceEqual(cachedFlags)) return cached;
        if (!quests.TryGetValue(progress.QuestId, out var current)) return null;
        var memo = new Dictionary<uint, bool>();
        bool IsCompleted(uint id)
        {
            if (!memo.TryGetValue(id, out var result)) memo[id] = result = completed(id);
            return result;
        }
        cached = new(current.ExpansionId, current.ExpansionName,
            Count(GetScope(current.ExpansionId, false), progress.QuestId, IsCompleted),
            Count(GetScope(current.ExpansionId, true), progress.QuestId, IsCompleted));
        cachedContentId = contentId;
        cachedQuestId = progress.QuestId;
        cachedFlags = completionFlags.ToArray();
        return cached;
    }

    private Dictionary<uint, MainScenarioQuest> GetScope(uint expansion, bool baseCampaign)
    {
        var key = (expansion, baseCampaign);
        if (scopes.TryGetValue(key, out var scope)) return scope;
        var rows = quests.Values.Where(q => q.ExpansionId == expansion).ToArray();
        // The earliest main-scenario journal genre is the launch (.0) campaign, not later patch chapters.
        var baseGenre = rows.Select(q => q.GenreId).DefaultIfEmpty().Min();
        return scopes[key] = rows.Where(q => !baseCampaign || q.GenreId == baseGenre).ToDictionary(q => q.Id);
    }

    private static QuestCompletion? Count(Dictionary<uint, MainScenarioQuest> scope, uint current, Func<uint, bool> completed)
    {
        var ends = MainScenarioResolver.FindTerminalIds(scope.Values);
        if (ends.Length != 1) return null;
        // A cleared endpoint proves 100% even if older/reworked prerequisite flags are absent.
        // Do not invent quest counts for a route that can no longer be reconstructed.
        if (completed(ends[0])) return new(0, 0, true);
        var required = new HashSet<uint>();
        var visiting = new HashSet<uint>();

        HashSet<uint> Ancestors(uint start)
        {
            var found = new HashSet<uint>();
            var stack = new Stack<uint>(); stack.Push(start);
            while (stack.TryPop(out var id))
                if (scope.TryGetValue(id, out var q) && found.Add(id))
                    foreach (var previous in q.PreviousIds) stack.Push(previous);
            return found;
        }

        bool Visit(uint id)
        {
            if (!scope.TryGetValue(id, out var quest) || required.Contains(id)) return true;
            if (!visiting.Add(id)) return false;
            var previous = quest.PreviousIds.Where(scope.ContainsKey).Distinct().ToArray();
            if (quest.PreviousJoin == 2 && previous.Length > 1)
            {
                // OR prerequisites include the mutually exclusive starting-city/class routes.
                var done = previous.Where(completed).Order().ToArray();
                if (done.Length > 0) previous = [done[0]];
                else
                {
                    var scores = previous.Select(p =>
                    {
                        var ancestors = Ancestors(p);
                        return (Id: p, Current: ancestors.Contains(current), Done: ancestors.Count(completed));
                    }).OrderByDescending(x => x.Current).ThenByDescending(x => x.Done).ToArray();
                    if (scores[0].Current == scores[1].Current && scores[0].Done == scores[1].Done) return false;
                    previous = [scores[0].Id];
                }
            }
            foreach (var p in previous) if (!Visit(p)) return false;
            visiting.Remove(id);
            required.Add(id);
            return true;
        }

        if (!Visit(ends[0]) || required.Count == 0) return null;
        return new(required.Count(completed), required.Count);
    }
}
