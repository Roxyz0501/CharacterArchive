using CharacterArchive.Models;

namespace CharacterArchive.Services;

public static class LanguageResolver
{
    public static DisplayLanguage Initialize(
        DisplayLanguage? saved,
        string? gameLanguage,
        string? dalamudLanguage,
        string? launcherLanguage = null)
    {
        if (saved is { } savedValue && IsSupported(savedValue))
            return savedValue;

        return Normalize(gameLanguage)
            ?? Normalize(dalamudLanguage)
            ?? Normalize(launcherLanguage)
            ?? DisplayLanguage.English;
    }

    public static bool IsSupported(DisplayLanguage? language) => language is
        DisplayLanguage.English or DisplayLanguage.Japanese or DisplayLanguage.German or
        DisplayLanguage.French or DisplayLanguage.Korean or DisplayLanguage.SimplifiedChinese or
        DisplayLanguage.TraditionalChinese;

    public static DisplayLanguage? Normalize(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return null;

        var normalized = language.Trim().Replace('_', '-').ToLowerInvariant();
        return normalized switch
        {
            "japanese" or "ja" or "jp" => DisplayLanguage.Japanese,
            "english" or "en" => DisplayLanguage.English,
            "german" or "de" => DisplayLanguage.German,
            "french" or "fr" => DisplayLanguage.French,
            "korean" or "ko" => DisplayLanguage.Korean,
            "simplifiedchinese" or "chinesesimplified" or "zh-hans" or "zh-cn" or "zh-sg" => DisplayLanguage.SimplifiedChinese,
            "traditionalchinese" or "chinesetraditional" or "zh-hant" or "zh-tw" or "zh-hk" or "zh-mo" => DisplayLanguage.TraditionalChinese,
            _ when normalized.StartsWith("ja-") => DisplayLanguage.Japanese,
            _ when normalized.StartsWith("en-") => DisplayLanguage.English,
            _ when normalized.StartsWith("de-") => DisplayLanguage.German,
            _ when normalized.StartsWith("fr-") => DisplayLanguage.French,
            _ when normalized.StartsWith("ko-") => DisplayLanguage.Korean,
            _ => null,
        };
    }
}
