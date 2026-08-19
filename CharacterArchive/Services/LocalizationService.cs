using CharacterArchive.Models;

namespace CharacterArchive.Services;

public sealed class LocalizationService
{
    private readonly Configuration configuration;

    public LocalizationService(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public ResolvedLanguage Current => configuration.DisplayLanguage == DisplayLanguage.Japanese
        ? ResolvedLanguage.Japanese
        : ResolvedLanguage.English;

    public bool IsJapanese => Current == ResolvedLanguage.Japanese;

    public string Text(string english, string japanese) => IsJapanese ? japanese : english;

}
