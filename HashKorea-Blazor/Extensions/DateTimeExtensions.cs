namespace HashKorea.Extensions;

// DB default : UTC
// Get time based on Korea (default)
public static class DateTimeExtensions
{
    private static readonly TimeZoneInfo KoreaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");

    public static DateTime ConvertToKoreaTime(DateTime utcDateTime)
    {
        if (utcDateTime.Kind == DateTimeKind.Unspecified)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }

        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, KoreaTimeZone);
    }

    public static string ToRelativeTimeString(this DateTime utcDateTime)
    {
        // TO DO: local time 처리
        if (utcDateTime.Kind == DateTimeKind.Unspecified)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }

        DateTime koreaDateTime = ConvertToKoreaTime(utcDateTime);
        DateTime nowKoreaTime = ConvertToKoreaTime(DateTime.UtcNow);

        var timeSpan = nowKoreaTime - koreaDateTime;

        if (timeSpan.TotalMinutes < 1)
            return "just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hours ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} days ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";
        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)} months ago";

        return $"{(int)(timeSpan.TotalDays / 365)} years ago";
    }

}
