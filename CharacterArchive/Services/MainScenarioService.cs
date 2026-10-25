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

    public unsafe MainScenarioProgress? Capture()
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
                    q.PreviousQuest.Select(p => p.RowId).Where(id => id != 0).ToArray()))
                .ToDictionary(q => q.Id);
            terminalIds = MainScenarioResolver.FindTerminalIds(quests.Values);
        }
        var guide = agent->Data->MainScenarioQuestIds.ToArray();
        return MainScenarioResolver.Resolve(guide, agent->Data->MSQPathIndex, quests, terminalIds,
            id => QuestManager.IsQuestComplete(id), id => manager->IsQuestAccepted(id),
            id => QuestManager.GetQuestSequence(id), DateTime.Now);
    }
}
