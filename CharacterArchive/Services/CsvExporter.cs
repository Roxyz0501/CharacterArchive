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
        var path = Path.Combine(outputDirectory, $"character_file_mapping_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.csv");

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

        // This is an exported data schema, not UI text. Keep it stable across UI languages.
        _ = language;
        const string header = "キャラクター名,サーバー名,ロドストID,設定ファイル名";
        var lines = new List<string> { header };
        lines.AddRange(records
            .OrderBy(record => record.HomeWorld, StringComparer.OrdinalIgnoreCase)
            .ThenBy(record => record.CharacterName, StringComparer.OrdinalIgnoreCase)
            .Select(record => string.Join(',',
                Escape(record.CharacterName),
                Escape(record.HomeWorld),
                Escape(record.LodestoneId ?? string.Empty),
                Escape(record.ConfigFolderName))));

        var temporary = Path.GetFullPath(path) + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using (var writer = new StreamWriter(stream, new UTF8Encoding(true), leaveOpen: true))
                    foreach (var line in lines) writer.WriteLine(line);
                stream.Flush(true);
            }
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static string Escape(string value) =>
        $"\"{value.Replace("\"", "\"\"")}\"";
}
