using System.Globalization;

namespace OfficeBite.Mobile.Helpers;

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
        // If unspecified, treat as UTC
        return dt.Add(IstOffset);
    }

    /// <summary>
    /// Daily ordering cutoff time in IST (1:00 PM).
    /// </summary>
    public static readonly TimeOnly OrderCutoffTime = new TimeOnly(13, 0);

    public const string OrderTimeOverMessage = "Time is Over, Call Sanjeeb";

    /// <summary>
    /// Returns true if current Indian Standard Time is at or after the 1:00 PM cutoff.
    /// </summary>
    public static bool IsOrderTimeOver()
    {
        var time = TimeOnly.FromDateTime(NowIst);
        return time >= OrderCutoffTime;
    }

    /// <summary>
    /// Parses any date string (ISO UTC or local) into Indian Standard Time (IST).
    /// </summary>
    public static DateTime? ParseToIst(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr)) return null;

        if (DateTimeOffset.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
        {
            return dto.ToOffset(IstOffset).DateTime;
        }

        if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return dt.Kind == DateTimeKind.Utc ? dt.Add(IstOffset) : dt;
        }

        if (DateTime.TryParse(dateStr, out var dtFallback))
        {
            return dtFallback.Kind == DateTimeKind.Utc ? dtFallback.Add(IstOffset) : dtFallback;
        }

        return null;
    }

    /// <summary>
    /// Formats date string to IST full date and time (e.g. "30 Sep 2026, 01:45 PM").
    /// </summary>
    public static string FormatIstDateTime(string? dateStr)
    {
        var ist = ParseToIst(dateStr);
        return ist.HasValue ? ist.Value.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture) : dateStr ?? "";
    }

    /// <summary>
    /// Formats date string to IST time only (e.g. "01:45 PM").
    /// </summary>
    public static string FormatIstTime(string? dateStr)
    {
        var ist = ParseToIst(dateStr);
        return ist.HasValue ? ist.Value.ToString("hh:mm tt", CultureInfo.InvariantCulture) : "";
    }

    /// <summary>
    /// Formats current or given date to IST day format (e.g. "Wednesday, 30 September 2026").
    /// </summary>
    public static string FormatIstFullDate(DateTime? dt = null)
    {
        var date = dt ?? NowIst;
        return date.ToString("dddd, dd MMMM yyyy", CultureInfo.InvariantCulture);
    }
}
