using System.Globalization;

namespace OfficeBite.Shared.Helpers;

public static class DateTimeHelper
{
    // Indian Standard Time is UTC + 5:30
    public static readonly TimeSpan IstOffset = TimeSpan.FromHours(5.5);

    /// <summary>
    /// Current DateTime in Indian Standard Time (IST).
    /// </summary>
    public static DateTime NowIst => DateTime.UtcNow.Add(IstOffset);

    /// <summary>
    /// Current Date in Indian Standard Time (IST).
    /// </summary>
    public static DateOnly TodayIst => DateOnly.FromDateTime(NowIst);

    /// <summary>
    /// Converts a DateTime to Indian Standard Time (IST).
    /// </summary>
    public static DateTime ToIst(this DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Utc)
            return dt.Add(IstOffset);
        if (dt.Kind == DateTimeKind.Local)
            return dt.ToUniversalTime().Add(IstOffset);
        return dt.Add(IstOffset);
    }
}
