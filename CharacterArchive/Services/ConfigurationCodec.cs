using System.Text.Json;

namespace CharacterArchive.Services;

internal static class ConfigurationCodec
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public static string Encode(Configuration value) => JsonSerializer.Serialize(value, Options);

    public static Configuration Decode(string text)
    {
        using var document = JsonDocument.Parse(text);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("Version", out var version) ||
            version.ValueKind != JsonValueKind.Number ||
            !version.TryGetInt32(out var number) || number < 1 ||
            !document.RootElement.TryGetProperty("Characters", out var characters) ||
            characters.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Unsupported or incomplete Character Archive configuration.");
        if (number > 4)
            throw new NotSupportedException("This configuration was written by a newer plugin. Update the plugin before loading it.");
        // Legacy Dalamud TypeNameHandling.Objects metadata is ignored; never resolve arbitrary CLR types.
        var value = JsonSerializer.Deserialize<Configuration>(text, Options)
                    ?? throw new InvalidDataException("Null configuration.");
        if (value.Characters is null || value.Characters.Any(c => c is null || c.Jobs is null || c.Jobs.Any(j => j is null)) ||
            value.Characters.Select(c => c.ContentId).Distinct().Count() != value.Characters.Count)
            throw new InvalidDataException("Invalid character collection.");
        foreach (var c in value.Characters)
        {
            if (c.ContentId == 0 || string.IsNullOrWhiteSpace(c.CharacterName) || string.IsNullOrWhiteSpace(c.HomeWorld) ||
                c.Jobs.Any(j => j.Level < 0 || j.Name is null || j.Abbreviation is null) ||
                c.Jobs.Select(j => j.RowId).Distinct().Count() != c.Jobs.Count ||
                (c.MainScenario is { } msq && (msq.QuestId == 0 || string.IsNullOrWhiteSpace(msq.QuestName) ||
                    msq.Chapter is null || !Enum.IsDefined(msq.Status))))
                throw new InvalidDataException("Invalid character or main-scenario data.");
        }
        return value;
    }
}
