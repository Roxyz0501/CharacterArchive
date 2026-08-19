using System.Text;
using CharacterArchive.Models;

namespace CharacterArchive.Services;

public static class CsvExporter
{
    public static string Export(
        IEnumerable<CharacterRecord> records,
        string outputDirectory,
        ResolvedLanguage language = ResolvedLanguage.Japanese)
    {
        Directory.CreateDirectory(outputDirectory);
        var path = Path.Combine(outputDirectory, $"character_file_mapping_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        ExportToPath(records, path, language);
        return path;
    }

    public static void ExportToPath(
        IEnumerable<CharacterRecord> records,
        string path,
        ResolvedLanguage language = ResolvedLanguage.Japanese)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var header = language == ResolvedLanguage.Japanese
            ? "キャラクター名,サーバー名,ロドストID,設定ファイル名"
            : "Character Name,Server,Lodestone ID,Configuration Folder";
        var lines = new List<string> { header };
        lines.AddRange(records
            .OrderBy(record => record.HomeWorld, StringComparer.OrdinalIgnoreCase)
            .ThenBy(record => record.CharacterName, StringComparer.OrdinalIgnoreCase)
            .Select(record => string.Join(',',
                Escape(record.CharacterName),
                Escape(record.HomeWorld),
                Escape(record.LodestoneId ?? string.Empty),
                Escape(record.ConfigFolderName))));

        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string Escape(string value) =>
        $"\"{value.Replace("\"", "\"\"")}\"";
}
