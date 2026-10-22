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
            !version.TryGetInt32(out var number) || number < 1 || number > 4 ||
            !document.RootElement.TryGetProperty("Characters", out var characters) ||
            characters.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Unsupported or incomplete Character Archive configuration.");
        // Legacy Dalamud TypeNameHandling.Objects metadata is ignored; never resolve arbitrary CLR types.
        var value = JsonSerializer.Deserialize<Configuration>(text, Options)
                    ?? throw new InvalidDataException("Null configuration.");
        if (value.Characters is null || value.Characters.Any(c => c is null || c.Jobs is null || c.Jobs.Any(j => j is null)) ||
            value.Characters.Select(c => c.ContentId).Distinct().Count() != value.Characters.Count)
            throw new InvalidDataException("Invalid character collection.");
        return value;
    }
}
