using CharacterArchive.Models;

namespace CharacterArchive.Services;

public sealed class LocalizationService
{
    private readonly Configuration configuration;

    public LocalizationService(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public ResolvedLanguage Current => configuration.DisplayLanguage switch
    {
        DisplayLanguage.Japanese => ResolvedLanguage.Japanese,
        DisplayLanguage.German => ResolvedLanguage.German,
        DisplayLanguage.French => ResolvedLanguage.French,
        DisplayLanguage.Korean => ResolvedLanguage.Korean,
        DisplayLanguage.SimplifiedChinese => ResolvedLanguage.SimplifiedChinese,
        DisplayLanguage.TraditionalChinese => ResolvedLanguage.TraditionalChinese,
        _ => ResolvedLanguage.English,
    };

    public string Text(string english, string japanese)
    {
        if (Current == ResolvedLanguage.English)
            return english;
        if (LocalizationCatalog.TryGet(english, Current, out var translated))
            return translated;
        return Current == ResolvedLanguage.Japanese ? japanese : $"[{english}]";
    }

    public string Format(string englishFormat, string japaneseFormat, params object[] arguments) =>
        string.Format(Text(englishFormat, japaneseFormat), arguments);

}
