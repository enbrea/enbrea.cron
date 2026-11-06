#region Enbrea.Cron - Copyright (c) STÜBER SYSTEMS GmbH
/*    
 *    Enbrea.Cron 
 *    
 *    Copyright (c) STÜBER SYSTEMS GmbH
 *
 *    Licensed under the MIT License.
 */
#endregion

using System.Globalization;

namespace Enbrea.Cron.Descriptor;

/// <summary>
/// Provides localized, human-readable text for supported cron expressions. 
/// </summary>
/// <remarks>
/// Handles common 5- to 7-field cron patterns and returns specific descriptions for recognized schedules (such as 
/// every minute, daily, weekdays, weekends, weekly, monthly, and yearly). Returns a generic localized description 
/// when the expression includes unsupported parts (for example, nonzero seconds or a specific year) or does not 
/// match a known pattern.
/// </remarks>
internal static class CronExpressionFormatter
{
    /// <summary>
    /// Formats a cron expression as a localized, human-readable schedule description.
    /// </summary>
    /// <remarks>
    /// Supports 5- to 7-field cron expressions but only formats patterns without seconds and without a year value. 
    /// Falls back to a generic localized description when parsing does not match a supported pattern.
    /// </remarks>
    /// <param name="expression">Cron expression to format.</param>
    /// <param name="options">Formatting options, including the target language.</param>
    /// <returns>A localized description for supported cron patterns, or a generic expression description when the 
    /// pattern is unsupported.</returns>
    internal static string Format(CronExpression expression, CronDescriptorOptions options)
    {
        var language = DescriptorTextProvider.ResolveLanguage(options?.Language);
        var culture = DescriptorTextProvider.GetCulture(language);

        var cron = expression.ToString();
        var fields = cron.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (fields.Length is < 5 or > 7)
        {
            return DescriptorTextProvider.Format("Expression_Generic", language, cron);
        }

        var hasSeconds = fields.Length >= 6;
        var hasYear = fields.Length == 7;

        var second = hasSeconds ? fields[0] : "0";
        var minute = fields[hasSeconds ? 1 : 0];
        var hour = fields[hasSeconds ? 2 : 1];
        var dayOfMonth = fields[hasSeconds ? 3 : 2];
        var month = fields[hasSeconds ? 4 : 3];
        var dayOfWeek = fields[hasSeconds ? 5 : 4];
        var year = hasYear ? fields[6] : "*";

        if (second != "0" || year != "*")
        {
            return DescriptorTextProvider.Format("Expression_Generic", language, cron);
        }

        if (minute == "*" && hour == "*" && dayOfMonth == "*" && month == "*" && dayOfWeek == "*")
        {
            return DescriptorTextProvider.Get("Expression_EveryMinute", language);
        }

        if (TryParseNumber(minute, 0, 59, out var parsedMinute) && hour == "*" && dayOfMonth == "*" && month == "*" && dayOfWeek == "*")
        {
            return DescriptorTextProvider.Format("Expression_EveryHourAtMinute", language, parsedMinute);
        }

        if (TryParseTime(hour, minute, out var timeText) && dayOfMonth == "*" && month == "*" && dayOfWeek == "*")
        {
            return DescriptorTextProvider.Format("Expression_EveryDayAtTime", language, timeText);
        }

        if (TryParseTime(hour, minute, out timeText) && dayOfMonth == "*" && month == "*" && dayOfWeek == "1-5")
        {
            return DescriptorTextProvider.Format("Expression_WeekdaysAtTime", language, timeText);
        }

        if (TryParseTime(hour, minute, out timeText) && dayOfMonth == "*" && month == "*" && dayOfWeek == "6,7")
        {
            return DescriptorTextProvider.Format("Expression_WeekendsAtTime", language, timeText);
        }

        if (TryParseTime(hour, minute, out timeText) && dayOfMonth == "*" && month == "*" && TryParseDayOfWeek(dayOfWeek, culture, out var dayName))
        {
            return DescriptorTextProvider.Format("Expression_EveryWeekOnDayAtTime", language, dayName, timeText);
        }

        if (TryParseTime(hour, minute, out timeText) && TryParseNumber(dayOfMonth, 1, 31, out var parsedDayOfMonth) && month == "*" && dayOfWeek == "*")
        {
            return DescriptorTextProvider.Format("Expression_EveryMonthOnDayAtTime", language, parsedDayOfMonth, timeText);
        }

        if (TryParseTime(hour, minute, out timeText) && TryParseNumber(dayOfMonth, 1, 31, out parsedDayOfMonth) && TryParseNumber(month, 1, 12, out var parsedMonth) && dayOfWeek == "*")
        {
            var monthName = culture.DateTimeFormat.GetMonthName(parsedMonth);
            return DescriptorTextProvider.Format("Expression_EveryYearOnMonthDayAtTime", language, monthName, parsedDayOfMonth, timeText);
        }

        return DescriptorTextProvider.Format("Expression_Generic", language, cron);
    }

    /// <summary>
    /// Parses a numeric day-of-week value and resolves its localized day name.
    /// </summary>
    private static bool TryParseDayOfWeek(string value, CultureInfo culture, out string dayName)
    {
        dayName = null;
        if (!TryParseNumber(value, 0, 7, out var day))
        {
            return false;
        }

        day = day == 7 ? 0 : day;
        dayName = culture.DateTimeFormat.GetDayName((DayOfWeek)day);

        return true;
    }

    /// <summary>
    /// Converts a string to an integer using invariant culture and verifies that the result is within an inclusive
    /// range.
    /// </summary>
    private static bool TryParseNumber(string value, int min, int max, out int number)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number))
        {
            return false;
        }

        return number >= min && number <= max;
    }

    /// <summary>
    /// Parses a numeric hour and minute value and formats it as a time string.
    /// </summary>
    private static bool TryParseTime(string hourText, string minuteText, out string timeText)
    {
        timeText = null;

        if (!TryParseNumber(hourText, 0, 23, out var hour) || !TryParseNumber(minuteText, 0, 59, out var minute))
        {
            return false;
        }

        timeText = FormattableString.Invariant($"{hour:00}:{minute:00}");

        return true;
    }
}
