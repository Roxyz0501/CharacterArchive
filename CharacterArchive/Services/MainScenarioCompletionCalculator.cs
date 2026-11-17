using CharacterArchive.Models;

namespace CharacterArchive.Services;

/// <summary>Quest-count progress on the player's required route, not time or internal quest sequence.</summary>
internal sealed class MainScenarioCompletionCalculator(IReadOnlyDictionary<uint, MainScenarioQuest> quests)
{
    private ulong cachedContentId;
    private uint cachedQuestId;
    private bool cachedActive;
    private readonly Dictionary<uint, MainScenarioQuest> all = quests.ToDictionary(q => q.Key, q => q.Value);
    private byte[]? cachedFlags;
    private MainScenarioExpansion? cached;
    private readonly Dictionary<(uint Expansion, bool Base), Dictionary<uint, MainScenarioQuest>> scopes = [];

    public MainScenarioExpansion? Calculate(ulong contentId, MainScenarioProgress progress,
        ReadOnlySpan<byte> completionFlags, Func<uint, bool> completed)
    {
        if (cachedFlags is not null && cachedContentId == contentId && cachedQuestId == progress.QuestId &&
            cachedActive == (progress.Status == MainScenarioStatus.InProgress) && completionFlags.SequenceEqual(cachedFlags)) return cached;
        if (!quests.TryGetValue(progress.QuestId, out var current)) return null;
        var memo = new Dictionary<uint, bool>();
        bool IsCompleted(uint id)
        {
            if (!memo.TryGetValue(id, out var result)) memo[id] = result = completed(id);
            return result;
        }
        var launch = GetScope(current.ExpansionId, true);
        var isLaunch = launch.ContainsKey(current.Id);
        var patch = isLaunch ? $"{current.ExpansionId + 2}.0" : PatchFor(current.Id);
        var launchProgress = Count(launch, current.Id, IsCompleted);
        cached = new(current.ExpansionId, current.ExpansionName,
            Count(GetScope(current.ExpansionId, false), progress.QuestId, IsCompleted),
            launchProgress)
        {
            AllCampaigns = Count(all, progress.QuestId, IsCompleted, progress.Status == MainScenarioStatus.InProgress),
            Patch = patch,
            PatchProgress = isLaunch ? launchProgress : patch is null ? null : Count(
                GetScope(current.ExpansionId, false).Values.Where(q => PatchFor(q.Id) == patch).ToDictionary(q => q.Id),
                current.Id, IsCompleted, progress.Status == MainScenarioStatus.InProgress),
        };
        cachedActive = progress.Status == MainScenarioStatus.InProgress;
        cachedContentId = contentId;
        cachedQuestId = progress.QuestId;
        cachedFlags = completionFlags.ToArray();
        return cached;
    }

    // Low 16 bits of the game's MSQ row ID, not localized titles or journal genre order.
    private static string? PatchFor(uint id) => (id & 0xffff) switch
    {
        1175 or (>= 1189 and <= 1192) or (>= 3874 and <= 3878) => "2.1",
        (>= 1346 and <= 1347) or 1352 or 1356 or (>= 1358 and <= 1363) or (>= 3879 and <= 3882) => "2.2",
        (>= 1442 and <= 1443) or (>= 1446 and <= 1448) or 1453 or (>= 1456 and <= 1460) or (>= 3883 and <= 3885) => "2.3",
        (>= 52 and <= 54) or 57 or 62 or 69 or (>= 74 and <= 75) or (>= 77 and <= 78) or 82 or 84 or (>= 86 and <= 89) or (>= 3886 and <= 3887) => "2.4",
        (>= 363 and <= 366) or (>= 368 and <= 369) or 429 or 3888 => "2.5",
        (>= 370 and <= 373) or 391 or 418 or (>= 420 and <= 428) or 4591 => "2.55",
        (>= 2156 and <= 2163) => "3.1",
        (>= 2231 and <= 2241) => "3.2",
        (>= 2242 and <= 2247) => "3.3",
        (>= 2341 and <= 2350) => "3.4",
        (>= 2351 and <= 2355) => "3.5",
        (>= 2356 and <= 2359) => "3.56",
        (>= 2962 and <= 2972) => "4.1",
        (>= 3022 and <= 3029) => "4.2",
        (>= 3070 and <= 3076) => "4.3",
        (>= 3143 and <= 3149) => "4.4",
        (>= 3179 and <= 3183) => "4.5",
        (>= 3184 and <= 3185) => "4.56",
        (>= 3673 and <= 3682) => "5.1",
        (>= 3761 and <= 3770) => "5.2",
        (>= 3771 and <= 3782) => "5.3",
        (>= 4007 and <= 4016) => "5.4",
        (>= 4058 and <= 4063) => "5.5",
        (>= 4064 and <= 4066) => "5.55",
        (>= 4526 and <= 4535) => "6.1",
        (>= 4592 and <= 4600) => "6.2",
        (>= 4670 and <= 4678) => "6.3",
        (>= 4735 and <= 4743) => "6.4",
        (>= 4744 and <= 4750) => "6.5",
        (>= 4751 and <= 4753) => "6.55",
        (>= 5244 and <= 5250) => "7.1",
        (>= 5299 and <= 5306) => "7.2",
        (>= 5364 and <= 5373) => "7.3",
        (>= 5426 and <= 5434) => "7.4",
        (>= 5470 and <= 5474) => "7.5",
        (>= 5475 and <= 5478) => "7.56",
        _ => null,
    };

    private Dictionary<uint, MainScenarioQuest> GetScope(uint expansion, bool baseCampaign)
    {
        var key = (expansion, baseCampaign);
        if (scopes.TryGetValue(key, out var scope)) return scope;
        var rows = quests.Values.Where(q => q.ExpansionId == expansion).ToArray();
        // The earliest main-scenario journal genre is the launch (.0) campaign, not later patch chapters.
        var baseGenre = rows.Select(q => q.GenreId).DefaultIfEmpty().Min();
        return scopes[key] = rows.Where(q => !baseCampaign || q.GenreId == baseGenre).ToDictionary(q => q.Id);
    }

    private static QuestCompletion? Count(Dictionary<uint, MainScenarioQuest> scope, uint current, Func<uint, bool> completed, bool active = false)
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
        var inferred = active && required.Contains(current) ? Ancestors(current) : new HashSet<uint>();
        inferred.Remove(current);
        return new(required.Count(id => inferred.Contains(id) || completed(id)), required.Count);
    }
}
