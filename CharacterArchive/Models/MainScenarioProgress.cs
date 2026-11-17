namespace CharacterArchive.Models;

public enum MainScenarioStatus { InProgress = 1, Available = 2, LastCompleted = 3, UpToDate = 4 }

public sealed record MainScenarioProgress(uint QuestId, string QuestName, string Chapter,
    MainScenarioStatus Status, byte? Sequence, DateTime ObservedAt)
{
    public MainScenarioExpansion? ExpansionProgress { get; init; }
}

public sealed record MainScenarioExpansion(uint Id, string Name, QuestCompletion? Overall, QuestCompletion? BaseCampaign)
{
    public QuestCompletion? AllCampaigns { get; init; }
}

public sealed record QuestCompletion(int Completed, int Total, bool MilestoneOnly = false)
{
    // Truncate to one decimal: an unfinished campaign must never round up to 100%.
    [System.Text.Json.Serialization.JsonIgnore]
    public decimal Percent => MilestoneOnly ? 100m : Total > 0 ? Math.Floor(1000m * Completed / Total) / 10m : 0m;
}
