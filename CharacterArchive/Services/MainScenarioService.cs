using CharacterArchive.Models;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace CharacterArchive.Services;

/// <summary>Reads client-owned state only; never invokes quest actions, commands or network requests.</summary>
internal sealed class MainScenarioService(IDataManager data)
{
    private Dictionary<uint, MainScenarioQuest>? quests;
    private uint[] terminalIds = [];
    private MainScenarioCompletionCalculator? completionCalculator;

    public unsafe MainScenarioProgress? Capture(ulong contentId)
    {
        var manager = QuestManager.Instance();
        var agent = AgentScenarioTree.Instance();
        if (manager is null || agent is null || agent->Data is null) return null;
        if (quests is null)
        {
            quests = data.GetExcelSheet<Quest>()
                .Where(q => q.RowId != 0 && q.JournalGenre.IsValid && q.JournalGenre.Value.Icon == 61412 &&
                            !string.IsNullOrWhiteSpace(q.Name.ToString()))
                .Select(q => new MainScenarioQuest(q.RowId, q.Name.ToString(), q.JournalGenre.Value.Name.ToString(),
                    q.PreviousQuest.Select(p => p.RowId).Where(id => id != 0).ToArray(),
                    q.Expansion.RowId, q.Expansion.IsValid ? q.Expansion.Value.Name.ToString() : string.Empty,
                    q.JournalGenre.RowId, q.PreviousQuestJoin))
                .ToDictionary(q => q.Id);
            terminalIds = MainScenarioResolver.FindTerminalIds(quests.Values);
            completionCalculator = new MainScenarioCompletionCalculator(quests);
        }
        var guide = agent->Data->MainScenarioQuestIds.ToArray();
        var progress = MainScenarioResolver.Resolve(guide, agent->Data->MSQPathIndex, quests, terminalIds,
            id => QuestManager.IsQuestComplete(id), id => manager->IsQuestAccepted(id),
            id => QuestManager.GetQuestSequence(id), DateTime.Now);
        return progress is null ? null : progress with
        {
            ExpansionProgress = completionCalculator!.Calculate(contentId, progress, manager->CompletedQuests,
                id => QuestManager.IsQuestComplete(id)),
        };
    }
}
