namespace CharacterArchive.Models;

public enum MainScenarioStatus { InProgress = 1, Available = 2, LastCompleted = 3, UpToDate = 4 }

public sealed record MainScenarioProgress(uint QuestId, string QuestName, string Chapter,
    MainScenarioStatus Status, byte? Sequence, DateTime ObservedAt);
