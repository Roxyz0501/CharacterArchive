using CharacterArchive.Models;
using Dalamud.Configuration;

namespace CharacterArchive;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 3;
    public List<CharacterRecord> Characters { get; set; } = [];
    public bool AutoLookupLodestoneId { get; set; }
    public string CsvExportDirectory { get; set; } = string.Empty;
    public PlayTimeDisplayMode PlayTimeDisplayMode { get; set; } = PlayTimeDisplayMode.GameStyle;
    public int PlayTimeCommandDelaySeconds { get; set; } = 1;
    public bool AutoRequestPlayTimeOnLogin { get; set; }
    public DisplayLanguage? DisplayLanguage { get; set; }
}
