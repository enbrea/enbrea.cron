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

using System.Collections;

namespace Enbrea.Cron;

/// <summary>
/// Enumerates matching UTC occurrences within a half-open search interval
/// in ascending or descending order.
/// </summary>
internal sealed class CronOccurrenceEnumerable : IEnumerable<DateTimeOffset>
{
    private readonly CancellationToken _cancellationToken;
    private readonly CronExpression _cronExpression;
    private readonly CronMatcher _cronMatcher;
    private readonly int _firstMatchingHour;
    private readonly int _firstMatchingMinute;
    private readonly int _firstMatchingSecond;
    private readonly DateTimeOffset _fromInclusiveUtc;
    private readonly int _lastMatchingHour;
    private readonly int _lastMatchingMinute;
    private readonly int _lastMatchingSecond;
    private readonly CronOccurrenceOrder _occurrenceOrder;
    private readonly TimeZoneInfo _timeZone;
    private readonly CronTimeZoneMapper _timeZoneMapper;
    private readonly DateTimeOffset _untilExclusiveUtc;

    /// <summary>
    /// Initializes a new instance of the <see cref="CronOccurrenceEnumerable"/> class.
    /// </summary>
    /// <param name="cronExpression">The cron expression that defines the schedule for enumerating occurrences.</param>
    /// <param name="timeZone">The schedule time zone.</param>
    /// <param name="fromInclusiveUtc">The inclusive beginning of the search interval in UTC.</param>
    /// <param name="untilExclusiveUtc">The exclusive end of the search interval in UTC.</param>
    /// <param name="occurrenceOrder">Specifies the order in which occurrences are enumerated.</param>
    /// <param name="cancellationToken">A cancellation token used to cooperatively stop enumeration.</param>
    internal CronOccurrenceEnumerable(
        CronExpression cronExpression,
        TimeZoneInfo timeZone,
        DateTimeOffset fromInclusiveUtc,
        DateTimeOffset untilExclusiveUtc,
        CronOccurrenceOrder occurrenceOrder,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cronExpression);
        ArgumentNullException.ThrowIfNull(timeZone);

        if (!Enum.IsDefined(occurrenceOrder))
        {
            throw new ArgumentOutOfRangeException(nameof(occurrenceOrder));
        }

        if (untilExclusiveUtc < fromInclusiveUtc)
        {
            throw new ArgumentException("The end of the search interval must not precede its start.", nameof(untilExclusiveUtc));
        }

        _cronExpression = cronExpression;
        _cronMatcher = new CronMatcher(cronExpression, timeZone);
        _timeZone = timeZone;
		_timeZoneMapper = new CronTimeZoneMapper(timeZone);

		var requestedFromInclusiveUtc = fromInclusiveUtc.ToUniversalTime();
        var requestedUntilExclusiveUtc = untilExclusiveUtc.ToUniversalTime();

        GetSupportedUtcInterval(out var supportedFromInclusiveUtc, out var supportedUntilExclusiveUtc);

        _fromInclusiveUtc = requestedFromInclusiveUtc > supportedFromInclusiveUtc
            ? requestedFromInclusiveUtc
            : supportedFromInclusiveUtc;

        _untilExclusiveUtc = requestedUntilExclusiveUtc < supportedUntilExclusiveUtc
            ? requestedUntilExclusiveUtc
            : supportedUntilExclusiveUtc;

        _occurrenceOrder = occurrenceOrder;
        _cancellationToken = cancellationToken;
        _firstMatchingHour = _cronExpression.Hours.GetFirstMatchingValue(0, 23);
        _lastMatchingHour = _cronExpression.Hours.GetLastMatchingValue(0, 23);
        _firstMatchingMinute = _cronExpression.Minutes.GetFirstMatchingValue(0, 59);
        _lastMatchingMinute = _cronExpression.Minutes.GetLastMatchingValue(0, 59);
        _firstMatchingSecond = _cronExpression.HasSeconds ? _cronExpression.Seconds.GetFirstMatchingValue(0, 59) : 0;
        _lastMatchingSecond = _cronExpression.HasSeconds ? _cronExpression.Seconds.GetLastMatchingValue(0, 59) : 0;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the sequence of scheduled date and time values based on the configured cron expression.
    /// </summary>
    /// <returns>An enumerator that yields each scheduled occurrence as a <see cref="DateTimeOffset"/> in the
    /// configured occurrence order. The sequence is empty if no occurrences are found within the configured range.</returns>
    public IEnumerator<DateTimeOffset> GetEnumerator()
    {
        if (_occurrenceOrder == CronOccurrenceOrder.Ascending)
        {
            foreach (var occurrence in EnumerateAscendingOccurrences(_fromInclusiveUtc, _untilExclusiveUtc))
            {
                yield return occurrence;
            }
        }
        else 
        {
            foreach (var occurrence in EnumerateDescendingOccurrences(_fromInclusiveUtc, _untilExclusiveUtc))
            {
                yield return occurrence;
            }
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Local candidates represent wall-clock values in the configured time zone.
    /// Their <see cref="DateTime.Kind"/> is always <see cref="DateTimeKind.Unspecified"/>.
    /// </summary>
    private static DateTime AsUnspecified(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Returns the first canonical local date and time slot in a given year, which is January 1st at 00:00:00.
    /// </summary>
    private static DateTime GetFirstCanonicalSlotInYear(int year)
    {
        return AsUnspecified(new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));
    }

    /// <summary>
    /// Gets the supported lower local-calendar boundary for scheduling.
    /// </summary>
    private static DateTime GetSupportedLocalLowerBoundary()
    {
        return new DateTime(CronYearBounds.MinYear, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Gets the supported exclusive upper local-calendar boundary for scheduling.
    /// </summary>
    private static DateTime GetSupportedLocalUpperExclusiveBoundary()
    {
        return new DateTime(CronYearBounds.MaxYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Attempts to add a specified time span to a local date and time, returning false if the result would exceed the 
    /// valid range of DateTime values.
    /// </summary>
    private static bool TryAddLocal(DateTime value, TimeSpan delta, out DateTime result)
    {
        result = default;

        var ticks = value.Ticks;
        var tickDelta = delta.Ticks;

        if (tickDelta > 0 && ticks > DateTime.MaxValue.Ticks - tickDelta)
        {
            return false;
        }

        if (tickDelta < 0 && ticks < DateTime.MinValue.Ticks - tickDelta)
        {
            return false;
        }

        result = new DateTime(ticks + tickDelta, value.Kind);
        return true;
    }

	/// <summary>
	/// Attempts to move the candidate to the next matching minute, considering the configured cron expression and the current 
    /// candidate's date and time.
	/// </summary>
	private static bool TryMoveToNextMatchingMinute(DateTime candidate, CronField minutes, out DateTime nextCandidate)
    {
        nextCandidate = candidate;

        // Check for the next matching minute within the current hour.
        if (minutes.TryGetNextMatchingValue(candidate.Minute + 1, 0, 59, out var minute))
        {
            nextCandidate = new DateTime(candidate.Year, candidate.Month, candidate.Day, candidate.Hour, minute, 0, DateTimeKind.Unspecified);
            return true;
        }

        // If no matching minute is found in the current hour, roll over to the next hour and search for a matching minute.
        if (!TryAddLocal(candidate, TimeSpan.FromHours(1), out var nextHour))
        {
            return false;
        }

        // Check for the next matching minute within the next hour.
        if (minutes.TryGetNextMatchingValue(0, 0, 59, out minute))
        {
            nextCandidate = new DateTime(nextHour.Year, nextHour.Month, nextHour.Day, nextHour.Hour, minute, 0, DateTimeKind.Unspecified);
            return true;
        }

        return false;
    }

	/// <summary>
	/// Attempts to move the candidate to the next matching second, considering the configured cron expression and the current candidate's 
    /// date and time.
	/// </summary>
	private static bool TryMoveToNextMatchingSecond(DateTime candidate, CronField seconds, out DateTime nextCandidate)
    {
        nextCandidate = candidate;

        // Check for the next matching second within the current minute.
        if (seconds.TryGetNextMatchingValue(candidate.Second + 1, 0, 59, out var second))
        {
            nextCandidate = new DateTime(candidate.Year, candidate.Month, candidate.Day, candidate.Hour, candidate.Minute, second, DateTimeKind.Unspecified);
            return true;
        }

        // If no matching second is found in the current minute, roll over to the next minute and search for a matching second.
        var rolledMinute = new DateTime(candidate.Year, candidate.Month, candidate.Day, candidate.Hour, candidate.Minute, 0, DateTimeKind.Unspecified);
        return TryAddLocal(rolledMinute, TimeSpan.FromMinutes(1), out nextCandidate);
    }

	/// <summary>
	/// Attempts to move the candidate to the previous matching minute, considering the configured cron expression and the current 
    /// candidate's date and time.
	/// </summary>
	private static bool TryMoveToPreviousMatchingMinute(DateTime candidate, CronField minutes, bool hasSeconds, out DateTime previousCandidate)
    {
        previousCandidate = candidate;

        // Check for the previous matching minute within the current hour.
        if (minutes.TryGetPreviousMatchingValue(candidate.Minute - 1, 0, 59, out var minute))
        {
            previousCandidate = new DateTime(candidate.Year, candidate.Month, candidate.Day, candidate.Hour, minute, hasSeconds ? 59 : 0, DateTimeKind.Unspecified);
            return true;
        }

        // If no matching minute is found in the current hour, roll back to the previous hour and search for a matching minute.
        if (!TryAddLocal(candidate, TimeSpan.FromHours(-1), out var previousHour))
        {
            return false;
        }

        // Check for the previous matching minute within the previous hour.
        if (minutes.TryGetPreviousMatchingValue(59, 0, 59, out minute))
        {
            previousCandidate = new DateTime(previousHour.Year, previousHour.Month, previousHour.Day, previousHour.Hour, minute, hasSeconds ? 59 : 0, DateTimeKind.Unspecified);
            return true;
        }

        return false;
    }

	/// <summary>
	/// Attempts to move the candidate to the previous matching second, considering the configured cron expression and the current
    /// candidate's date and time.  
	/// </summary>
	private static bool TryMoveToPreviousMatchingSecond(DateTime candidate, CronField seconds, out DateTime previousCandidate)
    {
        previousCandidate = candidate;

        // Check for the previous matching second within the current minute.
        if (seconds.TryGetPreviousMatchingValue(candidate.Second - 1, 0, 59, out var second))
        {
            previousCandidate = new DateTime(candidate.Year, candidate.Month, candidate.Day, candidate.Hour, candidate.Minute, second, DateTimeKind.Unspecified);
            return true;
        }

        // If no matching second is found in the current minute, roll back to the previous minute and search for a matching second.
        var rolledMinute = new DateTime(candidate.Year, candidate.Month, candidate.Day, candidate.Hour, candidate.Minute, 59, DateTimeKind.Unspecified);
        return TryAddLocal(rolledMinute, TimeSpan.FromMinutes(-1), out previousCandidate);
    }
    
    /// <summary>
    /// Determines whether a specific day in a given year and month may match the configured cron expression.
    /// </summary>
    private bool DayMayMatch(int year, int month, int day)
    {
        var probe = new DateTime(year, month, day, _firstMatchingHour, _firstMatchingMinute, _firstMatchingSecond, DateTimeKind.Unspecified);
        return _cronMatcher.IsMatchLocal(probe);
    }

    /// <summary>
    /// Enumerates matching occurrences in ascending order within [fromInclusiveUtc, untilExclusiveUtc).
    /// </summary>
    private IEnumerable<DateTimeOffset> EnumerateAscendingOccurrences(
        DateTimeOffset fromInclusiveUtc,
        DateTimeOffset untilExclusiveUtc)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (fromInclusiveUtc >= untilExclusiveUtc)
        {
            yield break;
        }

        DateTimeOffset? lastYieldedUtc = null;
        var increment = _cronExpression.HasSeconds ? TimeSpan.FromSeconds(1) : TimeSpan.FromMinutes(1);
        var hasSeconds = _cronExpression.HasSeconds;
        var hasYearBounds = TryGetExpressionYearBounds(out var minExpressionYear, out var maxExpressionYear);

        if (!TryGetFirstLocalSlotAtOrAfter(fromInclusiveUtc, out var localCandidate))
        {
            yield break;
        }

        if (hasYearBounds)
        {
            if (localCandidate.Year > maxExpressionYear)
            {
                yield break;
            }

            if (localCandidate.Year < minExpressionYear)
            {
                localCandidate = GetFirstCanonicalSlotInYear(minExpressionYear);
            }
        }

        while (localCandidate.Year <= CronYearBounds.MaxYear)
        {
            if (hasYearBounds)
            {
                if (localCandidate.Year > maxExpressionYear)
                {
                    yield break;
                }

                if (localCandidate.Year < minExpressionYear)
                {
                    localCandidate = GetFirstCanonicalSlotInYear(minExpressionYear);
                }
            }

            _cancellationToken.ThrowIfCancellationRequested();

            if (!TryApplyCoarseJumpsAscending(ref localCandidate, hasSeconds))
            {
                yield break;
            }

            if (localCandidate.Year < CronYearBounds.MinYear || localCandidate.Year > CronYearBounds.MaxYear)
            {
                yield break;
            }

            if (!_cronMatcher.IsMatchLocal(localCandidate))
            {
                goto moveNextCandidate;
            }

            if (!_timeZoneMapper.TryResolveLocalTimeToUtc(localCandidate, out var utcCandidate))
            {
                goto moveNextCandidate;
            }

            if (utcCandidate >= untilExclusiveUtc)
            {
                yield break;
            }

            if (utcCandidate >= fromInclusiveUtc)
            {
                if (lastYieldedUtc.HasValue && utcCandidate <= lastYieldedUtc.Value)
                {
                    goto moveNextCandidate;
                }

                lastYieldedUtc = utcCandidate;
                yield return utcCandidate;
            }

        moveNextCandidate:
            if (!TryAddLocal(localCandidate, increment, out var nextLocalCandidate) || nextLocalCandidate <= localCandidate)
            {
                yield break;
            }

            localCandidate = AsUnspecified(nextLocalCandidate);
        }
    }

    /// <summary>
    /// Enumerates matching occurrences in descending order within [fromInclusiveUtc, untilExclusiveUtc).
    /// </summary>
    private IEnumerable<DateTimeOffset> EnumerateDescendingOccurrences(
        DateTimeOffset fromInclusiveUtc,
        DateTimeOffset untilExclusiveUtc)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (fromInclusiveUtc >= untilExclusiveUtc)
        {
            yield break;
        }

        DateTimeOffset? lastYieldedUtc = null;
        var decrement = _cronExpression.HasSeconds ? TimeSpan.FromSeconds(1) : TimeSpan.FromMinutes(1);
        var hasSeconds = _cronExpression.HasSeconds;
        var hasYearBounds = TryGetExpressionYearBounds(out var minExpressionYear, out var maxExpressionYear);

        if (!TryGetLastLocalSlotBefore(untilExclusiveUtc, out var localCandidate))
        {
            yield break;
        }

        if (hasYearBounds)
        {
            if (localCandidate.Year < minExpressionYear)
            {
                yield break;
            }

            if (localCandidate.Year > maxExpressionYear)
            {
                localCandidate = GetLastCanonicalSlotInYear(maxExpressionYear);
            }
        }

        while (localCandidate.Year >= CronYearBounds.MinYear)
        {
            if (hasYearBounds)
            {
                if (localCandidate.Year < minExpressionYear)
                {
                    yield break;
                }

                if (localCandidate.Year > maxExpressionYear)
                {
                    localCandidate = GetLastCanonicalSlotInYear(maxExpressionYear);
                }
            }

            _cancellationToken.ThrowIfCancellationRequested();

            if (!TryApplyCoarseJumpsDescending(ref localCandidate, hasSeconds))
            {
                yield break;
            }

            if (localCandidate.Year < CronYearBounds.MinYear || localCandidate.Year > CronYearBounds.MaxYear)
            {
                yield break;
            }

            if (!_cronMatcher.IsMatchLocal(localCandidate))
            {
                goto movePreviousCandidate;
            }

            if (!_timeZoneMapper.TryResolveLocalTimeToUtc(localCandidate, out var utcCandidate))
            {
                goto movePreviousCandidate;
            }

            if (utcCandidate < fromInclusiveUtc)
            {
                yield break;
            }

            if (utcCandidate >= untilExclusiveUtc)
            {
                goto movePreviousCandidate;
            }

            if (lastYieldedUtc.HasValue && utcCandidate >= lastYieldedUtc.Value)
            {
                goto movePreviousCandidate;
            }

            lastYieldedUtc = utcCandidate;
            yield return utcCandidate;

        movePreviousCandidate:
            if (!TryAddLocal(localCandidate, -decrement, out var previousLocalCandidate) || previousLocalCandidate >= localCandidate)
            {
                yield break;
            }

            localCandidate = AsUnspecified(previousLocalCandidate);
        }
    }

    /// <summary>
    /// Returns the last canonical local date and time slot in a given year, which is December 31st at 23:59:00 or 23:59:59 depending on whether the cron expression has seconds.
    /// </summary>
    private DateTime GetLastCanonicalSlotInYear(int year)
    {
        return AsUnspecified(new DateTime(year, 12, 31, 23, 59, _cronExpression.HasSeconds ? 59 : 0, DateTimeKind.Unspecified));
    }

    /// <summary>
    /// Converts the supported local scheduling boundaries to the equivalent UTC half-open interval.
    /// </summary>
    private void GetSupportedUtcInterval(out DateTimeOffset fromInclusiveUtc, out DateTimeOffset untilExclusiveUtc)
    {
        var supportedLocalFrom = GetSupportedLocalLowerBoundary();
        var supportedLocalUntil = GetSupportedLocalUpperExclusiveBoundary();

        if (!_timeZoneMapper.TryResolveLocalTimeToUtc(supportedLocalFrom, out fromInclusiveUtc))
        {
            throw new InvalidOperationException("Failed to resolve the supported lower local boundary to UTC.");
        }

        if (!_timeZoneMapper.TryResolveLocalTimeToUtc(supportedLocalUntil, out untilExclusiveUtc))
        {
            throw new InvalidOperationException("Failed to resolve the supported upper local boundary to UTC.");
        }
    }

    /// <summary>
    /// Determines whether a specific month in a given year may contain any days that match the configured cron expression.
    /// </summary>
    private bool MonthMayContainMatchingDay(int year, int month)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        for (var day = 1; day <= daysInMonth; day++)
        {
            if (DayMayMatch(year, month, day))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Normalizes a local wall-clock value to the precision of the cron expression,
    /// removing fractional ticks and, for five-field expressions, seconds.
    /// </summary>
    private DateTime NormalizeLocalCandidate(DateTime localTime)
    {
        var unspecifiedLocal = AsUnspecified(localTime);

        return new DateTime(
            unspecifiedLocal.Year,
            unspecifiedLocal.Month,
            unspecifiedLocal.Day,
            unspecifiedLocal.Hour,
            unspecifiedLocal.Minute,
            _cronExpression.HasSeconds ? unspecifiedLocal.Second : 0,
            DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Applies coarse local-calendar jumps in ascending direction across month, day, hour, minute, and second.
    /// </summary>
    private bool TryApplyCoarseJumpsAscending(ref DateTime candidate, bool hasSeconds)
    {
        // Jump to the next month that matches the configured month field.
        while (!_cronExpression.Months.Matches(candidate.Month, 1, 12))
        {
            _cancellationToken.ThrowIfCancellationRequested();

            // Jump to the first day of the next month.
            var monthStart = new DateTime(candidate.Year, candidate.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);

            // Add the number of days in the current month to move to the first day of the next month.
            if (!TryAddLocal(monthStart, TimeSpan.FromDays(DateTime.DaysInMonth(candidate.Year, candidate.Month)), out var nextMonthStart))
            {
                return false;
            }

            // Create a new candidate at the first matching hour, minute, and second of the next month.
            candidate = new DateTime(
                nextMonthStart.Year,
                nextMonthStart.Month,
                1,
                _firstMatchingHour,
                _firstMatchingMinute,
                hasSeconds ? _firstMatchingSecond : 0,
                DateTimeKind.Unspecified);
        }

        // Jump to the next month that may contain a matching day.
        while (!MonthMayContainMatchingDay(candidate.Year, candidate.Month))
        {
            _cancellationToken.ThrowIfCancellationRequested();

            // Jump to the first day of the next month.
            var monthStart = new DateTime(candidate.Year, candidate.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);

            // Add the number of days in the current month to move to the first day of the next month.
            if (!TryAddLocal(monthStart, TimeSpan.FromDays(DateTime.DaysInMonth(candidate.Year, candidate.Month)), out var nextMonthStart))
            {
                return false;
            }

            // Create a new candidate at the first matching hour, minute, and second of the next month.
            candidate = new DateTime(
                nextMonthStart.Year,
                nextMonthStart.Month,
                1,
                _firstMatchingHour,
                _firstMatchingMinute,
                hasSeconds ? _firstMatchingSecond : 0,
                DateTimeKind.Unspecified);
        }

        // Jump to the next day that matches the configured day field.
        while (!DayMayMatch(candidate.Year, candidate.Month, candidate.Day))
        {
            _cancellationToken.ThrowIfCancellationRequested();

            // Jump to the first day of the next day.
            var dayStart = new DateTime(candidate.Year, candidate.Month, candidate.Day, 0, 0, 0, DateTimeKind.Unspecified);

            // Add one day to move to the first day of the next day.
            if (!TryAddLocal(dayStart, TimeSpan.FromDays(1), out var nextDayStart))
            {
                return false;
            }

            // Create a new candidate at the first matching hour, minute, and second of the next day.
            candidate = new DateTime(
                nextDayStart.Year,
                nextDayStart.Month,
                nextDayStart.Day,
                _firstMatchingHour,
                _firstMatchingMinute,
                hasSeconds ? _firstMatchingSecond : 0,
                DateTimeKind.Unspecified);
        }

        // Jump to the next hour that matches the configured hour field.
        if (!_cronExpression.Hours.Matches(candidate.Hour, 0, 23))
        {
            if (!TryMoveToNextMatchingHour(candidate, hasSeconds, out var nextHourCandidate) || nextHourCandidate <= candidate)
            {
                return false;
            }

            candidate = nextHourCandidate;
        }

        // Jump to the next minute that matches the configured minute field.
        if (!_cronExpression.Minutes.Matches(candidate.Minute, 0, 59))
        {
            if (!TryMoveToNextMatchingMinute(candidate, _cronExpression.Minutes, out var nextMinuteCandidate) || nextMinuteCandidate <= candidate)
            {
                return false;
            }

            candidate = nextMinuteCandidate;
        }

        if (hasSeconds && !_cronExpression.Seconds.Matches(candidate.Second, 0, 59))
        {
            if (!TryMoveToNextMatchingSecond(candidate, _cronExpression.Seconds, out var nextSecondCandidate) || nextSecondCandidate <= candidate)
            {
                return false;
            }

            candidate = nextSecondCandidate;
        }

        return true;
    }

    /// <summary>
    /// Applies coarse local-calendar jumps in descending direction across month, day, hour, minute, and second.
    /// </summary>
    private bool TryApplyCoarseJumpsDescending(ref DateTime candidate, bool hasSeconds)
    {
        while (!_cronExpression.Months.Matches(candidate.Month, 1, 12))
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var monthStart = new DateTime(candidate.Year, candidate.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            if (!TryAddLocal(monthStart, TimeSpan.FromTicks(-1), out var previousMonthEnd))
            {
                return false;
            }

            candidate = new DateTime(
                previousMonthEnd.Year,
                previousMonthEnd.Month,
                previousMonthEnd.Day,
                _lastMatchingHour,
                _lastMatchingMinute,
                hasSeconds ? _lastMatchingSecond : 0,
                DateTimeKind.Unspecified);
        }

        while (!MonthMayContainMatchingDay(candidate.Year, candidate.Month))
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var monthStart = new DateTime(candidate.Year, candidate.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            if (!TryAddLocal(monthStart, TimeSpan.FromTicks(-1), out var previousMonthEnd))
            {
                return false;
            }

            candidate = new DateTime(
                previousMonthEnd.Year,
                previousMonthEnd.Month,
                previousMonthEnd.Day,
                _lastMatchingHour,
                _lastMatchingMinute,
                hasSeconds ? _lastMatchingSecond : 0,
                DateTimeKind.Unspecified);
        }

        while (!DayMayMatch(candidate.Year, candidate.Month, candidate.Day))
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var dayStart = new DateTime(candidate.Year, candidate.Month, candidate.Day, 0, 0, 0, DateTimeKind.Unspecified);
            if (!TryAddLocal(dayStart, TimeSpan.FromTicks(-1), out var previousDayEnd))
            {
                return false;
            }

            candidate = new DateTime(
                previousDayEnd.Year,
                previousDayEnd.Month,
                previousDayEnd.Day,
                _lastMatchingHour,
                _lastMatchingMinute,
                hasSeconds ? _lastMatchingSecond : 0,
                DateTimeKind.Unspecified);
        }

        if (!_cronExpression.Hours.Matches(candidate.Hour, 0, 23))
        {
            if (!TryMoveToPreviousMatchingHour(candidate, hasSeconds, out var previousHourCandidate) || previousHourCandidate >= candidate)
            {
                return false;
            }

            candidate = previousHourCandidate;
        }

        if (!_cronExpression.Minutes.Matches(candidate.Minute, 0, 59))
        {
            if (!TryMoveToPreviousMatchingMinute(candidate, _cronExpression.Minutes, hasSeconds, out var previousMinuteCandidate) || previousMinuteCandidate >= candidate)
            {
                return false;
            }

            candidate = previousMinuteCandidate;
        }

        if (hasSeconds && !_cronExpression.Seconds.Matches(candidate.Second, 0, 59))
        {
            if (!TryMoveToPreviousMatchingSecond(candidate, _cronExpression.Seconds, out var previousSecondCandidate) || previousSecondCandidate >= candidate)
            {
                return false;
            }

            candidate = previousSecondCandidate;
        }

        return true;
    }

    /// <summary>
    /// Attempts to determine the minimum and maximum years specified in the cron expression, if a year field is present.
    /// </summary>
    private bool TryGetExpressionYearBounds(out int minYear, out int maxYear)
    {
        minYear = default;
        maxYear = default;

        if (!_cronExpression.HasYear)
        {
            return false;
        }

        minYear = _cronExpression.Years.GetFirstMatchingValue(CronYearBounds.MinYear, CronYearBounds.MaxYear);
        maxYear = _cronExpression.Years.GetLastMatchingValue(CronYearBounds.MinYear, CronYearBounds.MaxYear);
        return true;
    }

    /// <summary>
    /// Attempts to determine the first canonical local slot at or after
    /// the specified UTC instant.
    /// </summary>
    private bool TryGetFirstLocalSlotAtOrAfter(DateTimeOffset fromInclusiveUtc, out DateTime localCandidate)
    {
        localCandidate = default;

        var increment = _cronExpression.HasSeconds ? TimeSpan.FromSeconds(1) : TimeSpan.FromMinutes(1);

        if (!_timeZoneMapper.TryResolveUtcTimeToLocal(fromInclusiveUtc, out var localFrom))
        {
            return false;
        }

        localFrom = AsUnspecified(localFrom);
        localCandidate = NormalizeLocalCandidate(localFrom);

        if (localCandidate < localFrom)
        {
            return TryAddLocal(localCandidate, increment, out localCandidate);
        }

        return true;
    }

    /// <summary>
    /// Attempts to determine the last canonical local slot before the specified UTC instant.
    /// </summary>
    private bool TryGetLastLocalSlotBefore(DateTimeOffset untilExclusiveUtc, out DateTime localCandidate)
    {
        localCandidate = default;

        var decrement = _cronExpression.HasSeconds ? TimeSpan.FromSeconds(1) : TimeSpan.FromMinutes(1);

        if (!_timeZoneMapper.TryResolveUtcTimeToLocal(untilExclusiveUtc, out var localUntilExclusive))
        {
            return false;
        }

        localUntilExclusive = AsUnspecified(localUntilExclusive);

        if (TryGetLastLocalSlotInEarlierOverlapBefore(untilExclusiveUtc, localUntilExclusive, decrement, out localCandidate))
        {
            return true;
        }

        localCandidate = NormalizeLocalCandidate(localUntilExclusive);

        if (localCandidate < localUntilExclusive)
        {
            return true;
        }

        return TryAddLocal(localCandidate, -decrement, out localCandidate);
    }

    /// <summary>
    /// Attempts to determine the last canonical local slot in an earlier overlap before the specified UTC instant.
    /// </summary>
    private bool TryGetLastLocalSlotInEarlierOverlapBefore(
        DateTimeOffset untilExclusiveUtc,
        DateTime localUntilExclusive,
        TimeSpan increment,
        out DateTime localCandidate)
    {
        localCandidate = default;

        if (_timeZone.Equals(TimeZoneInfo.Utc) || !_timeZone.IsAmbiguousTime(localUntilExclusive))
        {
            return false;
        }

        var offsets = _timeZone.GetAmbiguousTimeOffsets(localUntilExclusive);
        var laterOffset = offsets[0] > offsets[1] ? offsets[1] : offsets[0];

        var utcTicks = untilExclusiveUtc.ToUniversalTime().UtcDateTime.Ticks;
        var actualOffset = new TimeSpan(localUntilExclusive.Ticks - utcTicks);

        if (actualOffset != laterOffset)
        {
            return false;
        }

        localCandidate = NormalizeLocalCandidate(localUntilExclusive);

        if (!_timeZone.IsAmbiguousTime(localCandidate))
        {
            return false;
        }

        while (true)
        {
            if (!TryAddLocal(localCandidate, increment, out var nextCandidate) || nextCandidate <= localCandidate)
            {
                return false;
            }

            nextCandidate = AsUnspecified(nextCandidate);

            if (!_timeZone.IsAmbiguousTime(nextCandidate))
            {
                return true;
            }

            localCandidate = nextCandidate;
        }
    }

    /// <summary>
    /// Attempts to move the candidate to the next matching hour, considering the configured cron expression and the current candidate's date and time.
    /// </summary>
    private bool TryMoveToNextMatchingHour(DateTime candidate, bool hasSeconds, out DateTime nextCandidate)
    {
        nextCandidate = candidate;

        // Check for the next matching hour within the current day.
        if (_cronExpression.Hours.TryGetNextMatchingValue(candidate.Hour + 1, 0, 23, out var hour))
        {
            nextCandidate = new DateTime(
                candidate.Year,
                candidate.Month,
                candidate.Day,
                hour,
                _firstMatchingMinute,
                hasSeconds ? _firstMatchingSecond : 0,
                DateTimeKind.Unspecified);

            return true;
        }

        // If no matching hour is found in the current day, roll over to the next day and search for a matching hour.
        var dayStart = new DateTime(candidate.Year, candidate.Month, candidate.Day, 0, 0, 0, DateTimeKind.Unspecified);

        if (!TryAddLocal(dayStart, TimeSpan.FromDays(1), out var nextDayStart))
        {
            return false;
        }

        nextCandidate = new DateTime(
            nextDayStart.Year,
            nextDayStart.Month,
            nextDayStart.Day,
            _firstMatchingHour,
            _firstMatchingMinute,
            hasSeconds ? _firstMatchingSecond : 0,
            DateTimeKind.Unspecified);

        return nextCandidate > candidate;
    }

    /// <summary>
    /// Attempts to move the candidate to the previous matching hour, considering the configured cron expression and the current candidate's date and time.
    /// </summary>
    private bool TryMoveToPreviousMatchingHour(DateTime candidate, bool hasSeconds, out DateTime previousCandidate)
    {
        previousCandidate = candidate;

        // Check for the previous matching hour within the current day.
        if (_cronExpression.Hours.TryGetPreviousMatchingValue(candidate.Hour - 1, 0, 23, out var hour))
        {
            previousCandidate = new DateTime(
                candidate.Year,
                candidate.Month,
                candidate.Day,
                hour,
                _lastMatchingMinute,
                hasSeconds ? _lastMatchingSecond : 0,
                DateTimeKind.Unspecified);

            return true;
        }

        // If no matching hour is found in the current day, roll back to the previous day and search for a matching hour.
        var dayStart = new DateTime(candidate.Year, candidate.Month, candidate.Day, 0, 0, 0, DateTimeKind.Unspecified);

        if (!TryAddLocal(dayStart, TimeSpan.FromTicks(-1), out var previousDayEnd))
        {
            return false;
        }

        previousCandidate = new DateTime(
            previousDayEnd.Year,
            previousDayEnd.Month,
            previousDayEnd.Day,
            _lastMatchingHour,
            _lastMatchingMinute,
            hasSeconds ? _lastMatchingSecond : 0,
            DateTimeKind.Unspecified);

        return previousCandidate < candidate;
    }
}
