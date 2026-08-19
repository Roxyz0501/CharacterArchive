using CharacterArchive.Models;

namespace CharacterArchive.Services;

public static class LanguageResolver
{
    public static DisplayLanguage Initialize(
        DisplayLanguage? saved,
        string? dalamudLanguage,
        bool japaneseGameClient)
    {
        if (saved is DisplayLanguage.English or DisplayLanguage.Japanese)
            return saved.Value;

        return IsJapaneseLanguageCode(dalamudLanguage) || japaneseGameClient
            ? DisplayLanguage.Japanese
            : DisplayLanguage.English;
    }

    private static bool IsJapaneseLanguageCode(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return false;

        return language.Equals("ja", StringComparison.OrdinalIgnoreCase) ||
               language.StartsWith("ja-", StringComparison.OrdinalIgnoreCase) ||
               language.Equals("jp", StringComparison.OrdinalIgnoreCase);
    }
}
