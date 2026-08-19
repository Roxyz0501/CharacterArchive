using CharacterArchive.Models;

namespace CharacterArchive.Services;

public static class PlayTimeFormatter
{
    public static string Format(
        uint? totalMinutes,
        PlayTimeDisplayMode mode,
        ResolvedLanguage language = ResolvedLanguage.Japanese)
    {
        if (totalMinutes is null)
            return language == ResolvedLanguage.Japanese ? "未取得" : "Not acquired";

        var minutes = totalMinutes.Value;
        return mode == PlayTimeDisplayMode.TotalHours
            ? $"{minutes / 60:00000}:{minutes % 60:00}:00"
            : FormatGameStyle(minutes, language);
    }

    private static string FormatGameStyle(uint totalMinutes, ResolvedLanguage language)
    {
        var days = totalMinutes / (24 * 60);
        var hours = totalMinutes / 60 % 24;
        var minutes = totalMinutes % 60;
        return language == ResolvedLanguage.Japanese
            ? $"{days}日 {hours}時間 {minutes}分"
            : $"{days}d {hours}h {minutes}m";
    }
}
