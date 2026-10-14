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

using System.ComponentModel;

namespace Enbrea.Cron;

/// <summary>
/// Represents an immutable cron schedule consisting of a parsed expression and the time zone in   
/// which it is evaluated.
/// </summary>
public sealed class CronSchedule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CronSchedule"/> class.
    /// </summary>
    /// <param name="expression">The parsed cron expression.</param>
    /// <param name="timeZone">The configured time zone used to interpret the expression.</param>
    public CronSchedule(CronExpression expression, TimeZoneInfo timeZone)
    {
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        TimeZone = timeZone ?? throw new ArgumentNullException(nameof(timeZone));
    }

    /// <summary>
    /// Gets the parsed cron expression.
    /// </summary>
    public CronExpression Expression { get; }

    /// <summary>
    /// Gets the configured schedule time zone.
    /// </summary>
    public TimeZoneInfo TimeZone { get; }

    /// <summary>
    /// Parses a cron expression and binds it to the specified time zone.
    /// </summary>
    /// <param name="expression">The cron expression string to parse.</param>
    /// <param name="timeZone">The time zone used to interpret the parsed expression.</param>
    /// <returns>An immutable schedule instance.</returns>
    public static CronSchedule Parse(string expression, TimeZoneInfo timeZone)
    {
        return new CronSchedule(CronExpression.Parse(expression), timeZone);
    }

    /// <summary>
    /// Parses a cron expression and binds it to the specified time zone.
    /// </summary>
    /// <param name="expression">The cron expression string to parse.</param>
    /// <param name="timeZoneId">The time zone ID used to interpret the parsed expression.</param>
    /// <returns>An immutable schedule instance.</returns>
    public static CronSchedule Parse(string expression, string timeZoneId)
    {
        ArgumentException.ThrowIfNullOrEmpty(timeZoneId);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        if (timeZone == null)
        {
            throw new ArgumentException($"The time zone ID '{timeZoneId}' was not found.", nameof(timeZoneId));
        }

        return Parse(expression, timeZone);
    }

    /// <summary>
    /// Attempts to parse a cron expression and bind it to the specified time zone.
    /// </summary>
    /// <param name="expression">The cron expression string to parse.</param>
    /// <param name="timeZone">The time zone used to interpret the parsed expression.</param>
    /// <param name="schedule">When this method returns, contains the parsed schedule if successful;
    /// otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    public static bool TryParse(string expression, TimeZoneInfo timeZone, out CronSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        if (CronExpression.TryParse(expression, out var parsedExpression))
        {
            schedule = new CronSchedule(parsedExpression, timeZone);
            return true;
        }

        schedule = null;
        return false;
    }

    /// <summary>
    /// Attempts to parse a cron expression and bind it to the specified time zone.
    /// </summary>
    /// <param name="expression">The cron expression string to parse.</param>
    /// <param name="timeZoneId">The time zone ID used to interpret the parsed expression.</param>
    /// <param name="schedule">When this method returns, contains the parsed schedule if successful;
    /// otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    public static bool TryParse(string expression, string timeZoneId, out CronSchedule schedule)
    {
        ArgumentException.ThrowIfNullOrEmpty(timeZoneId);

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone))
        {
            schedule = null;
            return false;
        }

        return TryParse(expression, timeZone, out schedule);
    }

    /// <summary>
    /// Finds the first occurrence within the half-open interval <c>[fromInclusive, untilExclusive)</c>.
    /// </summary>
    /// <param name="fromInclusive">The inclusive beginning of the search interval.</param>
    /// <param name="untilExclusive">The exclusive end of the search interval.</param>
    /// <param name="cancellationToken">A cancellation token used to cooperatively stop the search.</param>
    /// <returns>The first matching occurrence in UTC, or <c>null</c> if no occurrence 
    /// exists.</returns>
    public DateTimeOffset? GetNextOccurrence(
        DateTimeOffset fromInclusive,
        DateTimeOffset untilExclusive,
        CancellationToken cancellationToken = default)
    {
        using var enumerator = GetOccurrences(
            fromInclusive, 
            untilExclusive, 
            CronOccurrenceOrder.Ascending, 
            cancellationToken).GetEnumerator();

        return enumerator.MoveNext() ? enumerator.Current : null;
    }

    /// <summary>
    /// Returns occurrences in the half-open interval <c>[fromInclusive, untilExclusive)</c>.
    /// </summary>
    /// <remarks>
    /// Occurrence calculation is restricted to the supported local calendar year range in the 
    /// configured schedule time zone. Requested intervals are internally intersected with that 
    /// range. If a matching local wall-clock time does not exist because of a daylight-saving-time 
    /// transition, that occurrence is skipped.
    /// </remarks>
    /// <param name="fromInclusive">The inclusive beginning of the search interval.</param>
    /// <param name="untilExclusive">The exclusive end of the search interval.</param>
    /// <param name="order">Specifies the order in which occurrences are enumerated.</param>
    /// <param name="cancellationToken">A cancellation token used to cooperatively stop the enumeration.</param>
    /// <returns>A lazy sequence of matching UTC occurrences in the requested order.</returns>
    public IEnumerable<DateTimeOffset> GetOccurrences(
        DateTimeOffset fromInclusive,
        DateTimeOffset untilExclusive,
        CronOccurrenceOrder order = CronOccurrenceOrder.Ascending,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(order))
        {
            throw new ArgumentOutOfRangeException(nameof(order));
        }

        var fromInclusiveUtc = fromInclusive.ToUniversalTime();
        var untilExclusiveUtc = untilExclusive.ToUniversalTime();

        if (untilExclusiveUtc < fromInclusiveUtc)
        {
            throw new ArgumentException("The end of the search interval must not precede its start.", nameof(untilExclusive));
        }

        if (fromInclusiveUtc == untilExclusiveUtc)
        {
            return [];
        }

        return new CronOccurrenceEnumerable(
            Expression,
            TimeZone,
            fromInclusiveUtc,
            untilExclusiveUtc,
            order,
            cancellationToken);
    }

    /// <summary>
    /// Finds the last occurrence within the half-open interval <c>[fromInclusive, untilExclusive)</c>.
    /// </summary>
    /// <param name="fromInclusive">The inclusive beginning of the search interval.</param>
    /// <param name="untilExclusive">The exclusive end of the search interval.</param>
    /// <param name="cancellationToken">A cancellation token used to cooperatively stop the search.</param>
    /// <returns>The last matching occurrence in UTC, or <c>null</c> if no occurrence exists.</returns>
    public DateTimeOffset? GetPreviousOccurrence(
        DateTimeOffset fromInclusive,
        DateTimeOffset untilExclusive,
        CancellationToken cancellationToken = default)
    {
        using var enumerator = GetOccurrences(
            fromInclusive, 
            untilExclusive, 
            CronOccurrenceOrder.Descending, 
            cancellationToken).GetEnumerator();

        return enumerator.MoveNext() ? enumerator.Current : null;
    }

    /// <summary>
    /// Determines whether the specified instant matches this schedule in its configured time zone.
    /// </summary>
    /// <param name="instant">The instant to evaluate. Its original offset does not determine the 
    /// schedule's time zone.</param>
    /// <returns>
    /// <c>true</c> if the instant matches this schedule; otherwise <c>false</c>.
    /// For ambiguous local wall-clock values, only the earlier UTC occurrence selected by the 
    /// schedule overlap policy is considered a match. Values outside the supported local year range 
    /// return <c>false</c>.
    /// </returns>
    public bool IsMatch(DateTimeOffset instant)
    {
        return new CronMatcher(Expression, TimeZone).IsMatch(instant);
    }
}
