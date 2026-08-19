namespace CharacterArchive.Models;

public sealed class CharacterRecord
{
    public ulong ContentId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public string HomeWorld { get; set; } = string.Empty;
    public uint HomeWorldId { get; set; }
    public string CurrentWorld { get; set; } = string.Empty;
    public uint CurrentWorldId { get; set; }
    public string? LodestoneId { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? ProfileImagePath { get; set; }
    public bool ProfileImageIsFace { get; set; }
    public string ConfigFolderName { get; set; } = string.Empty;
    public string ConfigFolderPath { get; set; } = string.Empty;
    public bool ConfigFolderExists { get; set; }
    public string Race { get; set; } = string.Empty;
    public string Tribe { get; set; } = string.Empty;
    public string Sex { get; set; } = string.Empty;
    public string ClassJob { get; set; } = string.Empty;
    public uint CurrentClassJobId { get; set; }
    public short Level { get; set; }
    public List<JobLevelRecord> Jobs { get; set; } = [];
    public string GrandCompany { get; set; } = string.Empty;
    public string GuardianDeity { get; set; } = string.Empty;
    public string StartTown { get; set; } = string.Empty;
    public uint? PlayTimeMinutes { get; set; }
    public DateTime? PlayTimeUpdatedAt { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}
