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
/// Maps instants and local wall-clock date/time values using a configured time zone.
/// </summary>
internal readonly struct CronTimeZoneMapper
{
    private readonly TimeZoneInfo _timeZoneInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="CronTimeZoneMapper"/> class.
    /// </summary>
    /// <param name="timeZoneInfo">The time zone to use for date and time conversions.</param>
    internal CronTimeZoneMapper(TimeZoneInfo timeZoneInfo)
    {
        ArgumentNullException.ThrowIfNull(timeZoneInfo);

        _timeZoneInfo = timeZoneInfo;
    }

    /// <summary>
    /// Attempts to resolve a local wall-clock date and time to UTC using the configured time zone.
    /// </summary>
    internal bool TryResolveLocalTimeToUtc(DateTime localTime, out DateTimeOffset utcTime)
    {
        utcTime = default;

        if (localTime.Kind != DateTimeKind.Unspecified)
        {
            throw new ArgumentException("The local time must have DateTimeKind.Unspecified.", nameof(localTime));
        }

        if (_timeZoneInfo.Equals(TimeZoneInfo.Utc))
        {
            utcTime = new DateTimeOffset(localTime, TimeSpan.Zero);
            return true;
        }

        if (_timeZoneInfo.IsInvalidTime(localTime))
        {
            return false;
        }

        TimeSpan offset;

        if (_timeZoneInfo.IsAmbiguousTime(localTime))
        {
            offset = SelectEarlierAmbiguousOffset(localTime);
        }
        else
        {
            offset = _timeZoneInfo.GetUtcOffset(localTime);
        }

        var utcTicks = localTime.Ticks - offset.Ticks;
        utcTime = new DateTimeOffset(new DateTime(utcTicks, DateTimeKind.Utc));
        return true;
    }

    /// <summary>
    /// Attempts to resolve a UTC instant to local wall-clock time.
    /// </summary>
    internal bool TryResolveUtcTimeToLocal(DateTimeOffset instant, out DateTime localTime)
    {
        localTime = default;

        var utcInstant = instant.ToUniversalTime();
        var utcTicks = utcInstant.UtcDateTime.Ticks;

        if (_timeZoneInfo.Equals(TimeZoneInfo.Utc))
        {
            localTime = new DateTime(utcTicks, DateTimeKind.Unspecified);
            return true;
        }

        var offset = _timeZoneInfo.GetUtcOffset(utcInstant);
        if (offset.Ticks > 0 && utcTicks > DateTime.MaxValue.Ticks - offset.Ticks)
        {
            return false;
        }

        if (offset.Ticks < 0 && utcTicks < DateTime.MinValue.Ticks - offset.Ticks)
        {
            return false;
        }

        localTime = new DateTime(utcTicks + offset.Ticks, DateTimeKind.Unspecified);
        return true;
    }

    /// <summary>
    /// Selects the offset corresponding to the earlier UTC occurrence of an ambiguous local time 
    /// in the configured time zone.
    /// </summary>
    private TimeSpan SelectEarlierAmbiguousOffset(DateTime ambiguousLocalTime)
    {
        var offsets = _timeZoneInfo.GetAmbiguousTimeOffsets(ambiguousLocalTime);

        return offsets[0] > offsets[1] ? offsets[0] : offsets[1];
    }
}
