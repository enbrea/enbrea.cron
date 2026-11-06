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

using System.Text;

namespace Enbrea.Cron;

/// <summary>
/// An immutable parsed Cron recurrence rule that is independent of a time zone.
/// </summary>
public sealed class CronExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CronExpression"/> class.
    /// </summary>
    /// <param name="minutes">The minutes field expression.</param>
    /// <param name="hours">The hours field expression.</param>
    /// <param name="dayOfMonth">The day-of-month field expression.</param>
    /// <param name="months">The month field expression.</param>
    /// <param name="dayOfWeek">The day-of-week field expression.</param>
    internal CronExpression(
        CronField minutes,
        CronField hours,
        CronField dayOfMonth,
        CronField months,
        CronField dayOfWeek)
        : this(null, minutes, hours, dayOfMonth, months, dayOfWeek, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CronExpression"/> class.
    /// </summary>
    /// <param name="seconds">The seconds field expression.</param>
    /// <param name="minutes">The minutes field expression.</param>
    /// <param name="hours">The hours field expression.</param>
    /// <param name="dayOfMonth">The day-of-month field expression.</param>
    /// <param name="months">The month field expression.</param>
    /// <param name="dayOfWeek">The day-of-week field expression.</param>
    internal CronExpression(
        CronField seconds,
        CronField minutes,
        CronField hours,
        CronField dayOfMonth,
        CronField months,
        CronField dayOfWeek)
        : this(seconds, minutes, hours, dayOfMonth, months, dayOfWeek, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CronExpression"/> class.
    /// </summary>
    /// <param name="seconds">The seconds field expression.</param>
    /// <param name="minutes">The minutes field expression.</param>
    /// <param name="hours">The hours field expression.</param>
    /// <param name="dayOfMonth">The day-of-month field expression.</param>
    /// <param name="months">The month field expression.</param>
    /// <param name="dayOfWeek">The day-of-week field expression.</param>
    /// <param name="years">The year field expression.</param>
    internal CronExpression(
        CronField seconds,
        CronField minutes,
        CronField hours,
        CronField dayOfMonth,
        CronField months,
        CronField dayOfWeek,
        CronField years)
    {
        Seconds = seconds;
        Minutes = minutes ?? throw new ArgumentNullException(nameof(minutes));
        Hours = hours ?? throw new ArgumentNullException(nameof(hours));
        DayOfMonth = dayOfMonth ?? throw new ArgumentNullException(nameof(dayOfMonth));
        Months = months ?? throw new ArgumentNullException(nameof(months));
        DayOfWeek = dayOfWeek ?? throw new ArgumentNullException(nameof(dayOfWeek));
        Years = years;
        HasSeconds = seconds is not null;
        HasYear = years is not null;
    }

    /// <summary>
    /// Gets a value indicating whether this expression contains a seconds field.
    /// </summary>
    public bool HasSeconds { get; }

    /// <summary>
    /// Gets a value indicating whether this expression contains a year field.
    /// </summary>
    public bool HasYear { get; }

    /// <summary>
    /// Gets the day-of-month field.
    /// </summary>
    internal CronField DayOfMonth { get; }

    /// <summary>
    /// Gets the day-of-week field.
    /// </summary>
    internal CronField DayOfWeek { get; }
    /// <summary>
    /// Gets the hours field.
    /// </summary>
    internal CronField Hours { get; }

    /// <summary>
    /// Gets the minutes field.
    /// </summary>
    internal CronField Minutes { get; }

    /// <summary>
    /// Gets the month field.
    /// </summary>
    internal CronField Months { get; }

    /// <summary>
    /// Gets the seconds field, or <c>null</c> for a five-field expression.
    /// </summary>
    internal CronField Seconds { get; }

    /// <summary>
    /// Gets the year field, or <c>null</c> when the expression has no year field.
    /// </summary>
    internal CronField Years { get; }

    /// <summary>
    /// Creates a new instance of <see cref="CronExpression"/> from the specified cron expression 
    /// string.
    /// </summary>
    /// <param name="expressionText">The cron expression string to parse. Must not be null or empty
    /// and must conform to the supported cron syntax.</param>
    /// <returns>A CronExpression instance representing the parsed cron expression.</returns>
    public static CronExpression Parse(string expressionText)
    {
        return CronParser.Parse(expressionText);
    }

    /// <summary>
    /// Attempts to parse the specified cron expression string into a <see cref="CronExpression"/> 
    /// instance.
    /// </summary>
    /// <param name="expressionText">The cron expression string to parse.</param>
    /// <param name="result">When this method returns, contains the parsed cron expression if the 
    /// parsing succeeded, or <c>null</c> if the parsing failed.</param>
    /// <returns><c>true</c> if the parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string expressionText, out CronExpression result)
    {
        return CronParser.TryParse(expressionText, out result);
    }

    /// <summary> 
    /// Calculates the start of the canonical Cron time slot containing the specified instant. 
    /// </summary> 
    /// <param name="instant">The instant to normalize. Its original offset does not affect the 
    /// result.</param>
    /// <returns>The UTC start of the containing time slot. Five-field expressions use minute 
    /// precision; expressions with a seconds field use second precision.</returns>
    public DateTimeOffset GetSlotUtc(DateTimeOffset instant)
    {
        var utc = instant.ToUniversalTime();
        var precision = HasSeconds ? TimeSpan.TicksPerSecond : TimeSpan.TicksPerMinute;
        var ticks = utc.Ticks - (utc.Ticks % precision);

        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    /// <summary>
    /// Returns the canonical cron expression. Symbolic month and weekday names, macros, and 
    /// original whitespace are not preserved.
    /// </summary>
    /// <returns>A string representation of the cron expression.</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();

        if (HasSeconds)
        {
            sb.Append(Seconds);
            sb.Append(' ');
        }

        sb.Append(Minutes);
        sb.Append(' ');
        sb.Append(Hours);
        sb.Append(' ');
        sb.Append(DayOfMonth);
        sb.Append(' ');
        sb.Append(Months);
        sb.Append(' ');
        sb.Append(DayOfWeek);

        if (HasYear)
        {
            sb.Append(' ');
            sb.Append(Years);
        }

        return sb.ToString();
    }
}
