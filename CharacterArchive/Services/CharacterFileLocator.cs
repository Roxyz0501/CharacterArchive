namespace CharacterArchive.Services;

public static class CharacterFileLocator
{
    public static string GetFolderName(ulong contentId) => $"FFXIV_CHR{contentId:X16}";

    public static string GetDefaultGameConfigRoot()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(documents, "My Games", "FINAL FANTASY XIV - A Realm Reborn");
    }

    public static string GetFolderPath(ulong contentId) =>
        Path.Combine(GetDefaultGameConfigRoot(), GetFolderName(contentId));
}
