using CharacterArchive.Models;
using Dalamud.Plugin;

namespace CharacterArchive.Services;

public sealed class CharacterRepository
{
    private readonly object syncRoot = new();
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly Configuration configuration;

    public CharacterRepository(IDalamudPluginInterface pluginInterface, Configuration configuration)
    {
        this.pluginInterface = pluginInterface;
        this.configuration = configuration;
    }

    public IReadOnlyList<CharacterRecord> Snapshot()
    {
        lock (syncRoot)
        {
            return configuration.Characters
                .Select(Clone)
                .OrderByDescending(record => record.LastSeenAt)
                .ToArray();
        }
    }

    public CharacterRecord Upsert(CharacterRecord captured)
    {
        lock (syncRoot)
        {
            var existing = configuration.Characters.FirstOrDefault(record => record.ContentId == captured.ContentId);
            if (existing is null)
            {
                configuration.Characters.Add(captured);
                existing = captured;
            }
            else
            {
                var lodestoneId = existing.LodestoneId;
                var profileImageUrl = existing.ProfileImageUrl;
                var profileImagePath = existing.ProfileImagePath;
                var profileImageIsFace = existing.ProfileImageIsFace;
                var playTimeMinutes = existing.PlayTimeMinutes;
                var playTimeUpdatedAt = existing.PlayTimeUpdatedAt;
                var firstSeen = existing.FirstSeenAt;
                Copy(captured, existing);
                existing.LodestoneId = lodestoneId;
                existing.ProfileImageUrl = profileImageUrl;
                existing.ProfileImagePath = profileImagePath;
                existing.ProfileImageIsFace = profileImageIsFace;
                existing.PlayTimeMinutes = playTimeMinutes;
                existing.PlayTimeUpdatedAt = playTimeUpdatedAt;
                existing.FirstSeenAt = firstSeen;
            }

            SaveUnsafe();
            return Clone(existing);
        }
    }

    public void SetPlayTime(ulong contentId, uint totalMinutes)
    {
        lock (syncRoot)
        {
            var record = configuration.Characters.FirstOrDefault(item => item.ContentId == contentId);
            if (record is null)
                return;

            record.PlayTimeMinutes = totalMinutes;
            record.PlayTimeUpdatedAt = DateTime.Now;
            SaveUnsafe();
        }
    }

    public void SetLodestoneId(ulong contentId, string? lodestoneId)
    {
        lock (syncRoot)
        {
            var record = configuration.Characters.FirstOrDefault(item => item.ContentId == contentId);
            if (record is null)
                return;

            record.LodestoneId = string.IsNullOrWhiteSpace(lodestoneId) ? null : lodestoneId.Trim();
            SaveUnsafe();
        }
    }

    public void SetLodestoneData(ulong contentId, string lodestoneId, string? imageUrl, string? imagePath)
    {
        lock (syncRoot)
        {
            var record = configuration.Characters.FirstOrDefault(item => item.ContentId == contentId);
            if (record is null)
                return;

            record.LodestoneId = lodestoneId;
            record.ProfileImageUrl = imageUrl;
            record.ProfileImagePath = imagePath;
            record.ProfileImageIsFace = !string.IsNullOrWhiteSpace(imagePath);
            SaveUnsafe();
        }
    }

    public void SaveSettings()
    {
        lock (syncRoot)
            SaveUnsafe();
    }

    private void SaveUnsafe() => pluginInterface.SavePluginConfig(configuration);

    private static CharacterRecord Clone(CharacterRecord source)
    {
        var clone = new CharacterRecord();
        Copy(source, clone);
        return clone;
    }

    private static void Copy(CharacterRecord source, CharacterRecord destination)
    {
        destination.ContentId = source.ContentId;
        destination.CharacterName = source.CharacterName;
        destination.HomeWorld = source.HomeWorld;
        destination.HomeWorldId = source.HomeWorldId;
        destination.CurrentWorld = source.CurrentWorld;
        destination.CurrentWorldId = source.CurrentWorldId;
        destination.LodestoneId = source.LodestoneId;
        destination.ProfileImageUrl = source.ProfileImageUrl;
        destination.ProfileImagePath = source.ProfileImagePath;
        destination.ProfileImageIsFace = source.ProfileImageIsFace;
        destination.ConfigFolderName = source.ConfigFolderName;
        destination.ConfigFolderPath = source.ConfigFolderPath;
        destination.ConfigFolderExists = source.ConfigFolderExists;
        destination.Race = source.Race;
        destination.Tribe = source.Tribe;
        destination.Sex = source.Sex;
        destination.ClassJob = source.ClassJob;
        destination.CurrentClassJobId = source.CurrentClassJobId;
        destination.Level = source.Level;
        destination.Jobs = source.Jobs.Select(job => new JobLevelRecord
        {
            RowId = job.RowId,
            Name = job.Name,
            Abbreviation = job.Abbreviation,
            Level = job.Level,
        }).ToList();
        destination.GrandCompany = source.GrandCompany;
        destination.GuardianDeity = source.GuardianDeity;
        destination.StartTown = source.StartTown;
        destination.PlayTimeMinutes = source.PlayTimeMinutes;
        destination.PlayTimeUpdatedAt = source.PlayTimeUpdatedAt;
        destination.FirstSeenAt = source.FirstSeenAt;
        destination.LastSeenAt = source.LastSeenAt;
    }
}
