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
            return language switch
            {
                ResolvedLanguage.Japanese => "未取得",
                ResolvedLanguage.German => "Nicht erfasst",
                ResolvedLanguage.French => "Non acquis",
                ResolvedLanguage.Korean => "미취득",
                ResolvedLanguage.SimplifiedChinese => "未获取",
                ResolvedLanguage.TraditionalChinese => "未取得",
                _ => "Not acquired",
            };

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
        return language switch
        {
            ResolvedLanguage.Japanese => $"{days}日 {hours}時間 {minutes}分",
            ResolvedLanguage.German => $"{days} T {hours} Std. {minutes} Min.",
            ResolvedLanguage.French => $"{days} j {hours} h {minutes} min",
            ResolvedLanguage.Korean => $"{days}일 {hours}시간 {minutes}분",
            ResolvedLanguage.SimplifiedChinese => $"{days}天 {hours}小时 {minutes}分钟",
            ResolvedLanguage.TraditionalChinese => $"{days}天 {hours}小時 {minutes}分鐘",
            _ => $"{days}d {hours}h {minutes}m",
        };
    }
}
