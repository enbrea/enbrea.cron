#region Enbrea.Cron - Copyright (c) STÜBER SYSTEMS GmbH
/*    
 *    Enbrea.Cron 
 *    
 *    Copyright (c) STÜBER SYSTEMS GmbH
 *
 *    Licensed under the MIT License.
 * 
 */
#endregion

namespace Enbrea.Cron;

/// <summary>
/// Evaluates instants and local wall-clock values against a cron expression.
/// </summary>
internal readonly struct CronMatcher
{
    private const int Sunday = 0;
    private const int SundayAlias = 7;

    private readonly CronExpression _expression;
    private readonly CronTimeZoneMapper _timeZoneMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CronMatcher"/> class.
    /// </summary>
    /// <param name="expression">A cron expression.</param>
    /// <param name="timeZone">The configured schedule time zone.</param>
    internal CronMatcher(CronExpression expression, TimeZoneInfo timeZone)
    {
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
        _timeZoneMapper = new CronTimeZoneMapper(timeZone ?? throw new ArgumentNullException(nameof(timeZone)));
    }

    /// <summary>
    /// Determines whether the specified instant matches the cron expression in the configured time zone.
    /// </summary>
    /// <param name="instant">The instant to evaluate. Its original offset does not determine the cron time zone.</param>
    /// <returns><c>true</c> if the corresponding local wall-clock time matches the schedule and maps to
    /// the selected UTC occurrence for ambiguous overlaps; otherwise, <c>false</c>.
    /// The method also returns <c>false</c> when the instant cannot be represented in the configured time zone.</returns>
    internal bool IsMatch(DateTimeOffset instant)
    {
        if (!_timeZoneMapper.TryResolveUtcTimeToLocal(instant, out var localTime))
        {
            return false;
        }

        if (!IsMatchLocal(localTime))
        {
            return false;
        }

        if (!_timeZoneMapper.TryResolveLocalTimeToUtc(localTime, out var selectedOccurrenceUtc))
        {
            return false;
        }

        return selectedOccurrenceUtc == instant.ToUniversalTime();
    }

    /// <summary>
    /// Determines whether the specified local time matches the current cron expression schedule.
    /// </summary>
    /// <remarks>
    /// This method evaluates each relevant field of the cron expression, including year, month, day, hour, minute, 
    /// and optionally second, to determine if the provided time matches the schedule. Special handling is applied 
    /// for day-of-month and day-of-week fields according to cron expression 
    /// semantics.
    /// </remarks>
    /// <param name="localTime">The local date and time to evaluate against the cron expression.</param>
    /// <returns>true if the specified local time satisfies all fields of the cron expression; otherwise, false.</returns>
    internal bool IsMatchLocal(DateTime localTime)
    {
        if (localTime.Year < CronYearBounds.MinYear || localTime.Year > CronYearBounds.MaxYear)
        {
            return false;
        }

        if (_expression.HasYear && !_expression.Years.Matches(localTime.Year, CronYearBounds.MinYear, CronYearBounds.MaxYear))
        {
            return false;
        }

        if (_expression.HasSeconds && !_expression.Seconds.Matches(localTime.Second, 0, 59))
        {
            return false;
        }

        if (!_expression.Minutes.Matches(localTime.Minute, 0, 59))
        {
            return false;
        }

        if (!_expression.Hours.Matches(localTime.Hour, 0, 23))
        {
            return false;
        }

        if (!_expression.Months.Matches(localTime.Month, 1, 12))
        {
            return false;
        }

        var dayOfMonthMatch = IsDayOfMonthFieldMatch(_expression.DayOfMonth, localTime.Day, localTime);
        var dayOfWeekMatch = IsDayOfWeekFieldMatch(_expression.DayOfWeek, NormalizeDayOfWeek((int)localTime.DayOfWeek), localTime);
        var dayOfMonthIsAny = _expression.DayOfMonth.IsAny;
        var dayOfWeekIsAny = _expression.DayOfWeek.IsAny;

        if (_expression.DayOfMonth.IsQuestion)
        {
            return dayOfWeekMatch;
        }

        if (_expression.DayOfWeek.IsQuestion)
        {
            return dayOfMonthMatch;
        }

        if (dayOfMonthIsAny && dayOfWeekIsAny)
        {
            return true;
        }

        if (dayOfMonthIsAny)
        {
            return dayOfWeekMatch;
        }

        if (dayOfWeekIsAny)
        {
            return dayOfMonthMatch;
        }

        return dayOfMonthMatch || dayOfWeekMatch;
    }

    /// <summary>
    /// Returns the day number of the last weekday (Monday through Friday) in the specified month and year.
    /// </summary>
    private static int GetLastWeekdayOfMonth(int year, int month)
    {
        var day = DateTime.DaysInMonth(year, month);
        while (day > 0)
        {
            var date = new DateTime(year, month, day);
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                return day;
            }

            day--;
        }

        return 1;
    }

    /// <summary>
    /// Calculates the day of the month that is the nearest weekday (Monday through Friday) 
    /// to the specified date.
    /// </summary>
    private static int GetNearestWeekdayOfMonth(int year, int month, int day)
    {
        var maxDay = DateTime.DaysInMonth(year, month);
        var requestedDay = Math.Clamp(day, 1, maxDay);
        var candidateDate = new DateTime(year, month, requestedDay);

        if (candidateDate.DayOfWeek == DayOfWeek.Saturday)
        {
            return requestedDay == 1 ? requestedDay + 2 : requestedDay - 1;
        }

        if (candidateDate.DayOfWeek == DayOfWeek.Sunday)
        {
            return requestedDay == maxDay ? requestedDay - 2 : requestedDay + 1;
        }

        return requestedDay;
    }

    /// <summary>
    /// Determines whether the specified day-of-month value matches any item in the day-of-month field.
    /// </summary>
    private static bool IsDayOfMonthFieldMatch(CronField cronField, int value, DateTime localTime)
    {
        var items = cronField.Items;

        for (var index = 0; index < items.Count; index++)
        {
            if (IsDayOfMonthItemMatch(items[index], value, localTime))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified day-of-month value matches a day-of-month cron field item.
    /// </summary>
    private static bool IsDayOfMonthItemMatch(CronFieldElement item, int value, DateTime localTime)
    {
        return item.Type switch
        {
            CronFieldElementType.Any => true,
            CronFieldElementType.Question => true,
            CronFieldElementType.Value => value == item.Value,
            CronFieldElementType.Range => value >= item.Start && value <= item.End,
            CronFieldElementType.Step => IsDayOfMonthStepMatch(item, value),
            CronFieldElementType.Last => IsLastDayOfMonth(value, localTime),
            CronFieldElementType.LastWeekday => IsLastWeekdayOfMonth(value, localTime),
            CronFieldElementType.NearestWeekday => IsNearestWeekday(item.Value, value, localTime),
            _ => false
        };
    }

    /// <summary>
    /// Determines whether the specified day-of-month value matches the given day-of-month step item.
    /// </summary>
    private static bool IsDayOfMonthStepMatch(CronFieldElement item, int value)
    {
        if (item.Step <= 0 || item.StepBase == null)
        {
            return false;
        }

        if (item.StepBase.Type == CronFieldElementType.Any)
        {
            return value >= 1 && value <= 31 && ((value - 1) % item.Step) == 0;
        }

        if (item.StepBase.Type == CronFieldElementType.Value)
        {
            var start = item.StepBase.Value;

            return value >= start
                && value <= 31
                && ((value - start) % item.Step) == 0;
        }

        if (item.StepBase.Type == CronFieldElementType.Range)
        {
            var start = item.StepBase.Start;
            var end = item.StepBase.End;

            return value >= start
                && value <= end
                && ((value - start) % item.Step) == 0;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified day-of-week value matches any item in the day-of-week field.
    /// </summary>
    private static bool IsDayOfWeekFieldMatch(CronField cronField, int value, DateTime localTime)
    {
        var items = cronField.Items;

        for (var index = 0; index < items.Count; index++)
        {
            if (IsDayOfWeekItemMatch(items[index], value, localTime))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified day-of-week value matches a day-of-week cron field item.
    /// </summary>
    private static bool IsDayOfWeekItemMatch(CronFieldElement item, int value, DateTime localTime)
    {
        return item.Type switch
        {
            CronFieldElementType.Any => true,
            CronFieldElementType.Question => true,
            CronFieldElementType.Value => value == NormalizeDayOfWeek(item.Value),
            CronFieldElementType.Range => TryGetWeekdayRangeOffset(value, item.Start, item.End, out _),
            CronFieldElementType.Step => IsDayOfWeekStepMatch(item, value),
            CronFieldElementType.LastDayOfWeek => IsLastDayOfWeek(item.Value, value, localTime),
            CronFieldElementType.Hash => IsHashDayOfWeek(item.Value, item.Occurrence, value, localTime),
            _ => false
        };
    }

    /// <summary>
    /// Determines whether the specified day-of-week value matches the given day-of-week step item.
    /// </summary>
    private static bool IsDayOfWeekStepMatch(CronFieldElement item, int value)
    {
        if (item.Step <= 0 || item.StepBase == null)
        {
            return false;
        }

        if (item.StepBase.Type == CronFieldElementType.Any)
        {
            if (value >= Sunday && ((value - Sunday) % item.Step) == 0)
            {
                return true;
            }

            return value == Sunday && ((SundayAlias - Sunday) % item.Step) == 0;
        }

        if (item.StepBase.Type == CronFieldElementType.Value)
        {
            var start = NormalizeDayOfWeek(item.StepBase.Value);
            if (start == Sunday)
            {
                return value % item.Step == 0;
            }

            var comparableValue = value == Sunday
                ? SundayAlias
                : value;

            return comparableValue >= start && ((comparableValue - start) % item.Step) == 0;
        }

        if (item.StepBase.Type == CronFieldElementType.Range)
        {
            return TryGetWeekdayRangeOffset(value, item.StepBase.Start, item.StepBase.End, out var offset)
                && (offset % item.Step) == 0;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified date and day of week match a cron expression using the '#' (nth occurrence)
    /// syntax.
    /// </summary>
    private static bool IsHashDayOfWeek(int dayOfWeekValue, int occurrence, int value, DateTime localTime)
    {
        var normalizedDayOfWeek = NormalizeDayOfWeek(dayOfWeekValue);
        if (value != normalizedDayOfWeek)
        {
            return false;
        }

        var currentOccurrence = ((localTime.Day - 1) / 7) + 1;

        return currentOccurrence == occurrence;
    }
    /// <summary>
    /// Determines whether the specified value represents the last day of the month for the given date and cron field
    /// type.
    /// </summary>
    private static bool IsLastDayOfMonth(int value, DateTime localTime)
    {
        return value == DateTime.DaysInMonth(localTime.Year, localTime.Month);
    }

    /// <summary>
    /// Determines whether the specified date represents the last occurrence of the specified day of the week within 
    /// the month.
    /// </summary>
    private static bool IsLastDayOfWeek(int dayOfWeekValue, int value, DateTime localTime)
    {
        var normalizedDayOfWeek = NormalizeDayOfWeek(dayOfWeekValue);

        return value == normalizedDayOfWeek && localTime.Day + 7 > DateTime.DaysInMonth(localTime.Year, localTime.Month);
    }

    /// <summary>
    /// Determines whether the specified day value represents the last weekday (Monday through Friday) of the month
    /// for the given date.
    /// </summary>
    private static bool IsLastWeekdayOfMonth(int value, DateTime localTime)
    {
        return value == GetLastWeekdayOfMonth(localTime.Year, localTime.Month);
    }

    /// <summary>
    /// Determines whether the specified value represents the nearest weekday (Monday through Friday) to the given 
    /// day of the month in the provided local time context.
    /// </summary>
    private static bool IsNearestWeekday(int day, int value, DateTime localTime)
    {
        return value == GetNearestWeekdayOfMonth(localTime.Year, localTime.Month, day);
    }
    /// <summary>
    /// Normalizes the specified day-of-week value to ensure a consistent representation of Sunday.
    /// </summary>
    private static int NormalizeDayOfWeek(int value)
    {
        return value == SundayAlias ? Sunday : value;
    }

    /// <summary>
    /// Tries to determine the zero-based offset of a weekday within a weekday range.
    /// </summary>
    private static bool TryGetWeekdayRangeOffset(int value, int start, int end, out int offset)
    {
        // Normalize Sunday aliases: both 0 and 7 become the same logical weekday.
        var normalizedValue = NormalizeDayOfWeek(value);
        var normalizedStart = NormalizeDayOfWeek(start);
        var normalizedEnd = NormalizeDayOfWeek(end);

        // Calculate the candidate's zero-based position relative to the range start.
        // The modulo operation supports wrapped ranges such as FRI-MON.
        offset = (normalizedValue - normalizedStart + 7) % 7;

        int lastOffset;

        if (normalizedStart == normalizedEnd)
        {
            // Identical original values, such as SUN-SUN, represent one weekday.
            // Different aliases of Sunday, such as 0-7 or 7-0, represent a full week.
            lastOffset = start == end ? 0 : 6;
        }
        else
        {
            // Calculate the zero-based offset of the inclusive range end.
            // Example: FRI-MON produces an end offset of 3.
            lastOffset = (normalizedEnd - normalizedStart + 7) % 7;
        }

        // The value belongs to the range when its offset does not exceed the end.
        return offset <= lastOffset;
    }
}
