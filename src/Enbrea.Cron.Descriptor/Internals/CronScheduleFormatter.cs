#region Enbrea.Cron - Copyright (c) STÜBER SYSTEMS GmbH
/*    
 *    Enbrea.Cron 
 *    
 *    Copyright (c) STÜBER SYSTEMS GmbH
 *
 *    Licensed under the MIT License.
 */
#endregion

namespace Enbrea.Cron.Descriptor;

/// <summary>
/// Formats a <see cref="CronSchedule"/> into a localized, human-readable description that includes the cron 
/// expression, time zone, and relative next and previous occurrences.
/// </summary>
/// <remarks>
/// Uses descriptor language and reference time options when provided; otherwise uses defaults.
/// </remarks>
internal static class CronScheduleFormatter
{
    /// <summary>
    /// Creates a localized description of a cron schedule, including the cron expression, time zone, and relative 
    /// previous and next occurrences.
    /// </summary>
    /// <param name="schedule">The cron schedule to describe.</param>
    /// <param name="options">Formatting options, including language and reference UTC time. If null, default options are used.</param>
    /// <returns>A formatted schedule description that combines expression text, time zone, and previous and next occurrence
    /// information.</returns>
    internal static string Format(CronSchedule schedule, CronDescriptorOptions options)
    {
        var language = DescriptorTextProvider.ResolveLanguage(options?.Language);
        var referenceUtc = options?.ReferenceTimeUtc ?? DateTimeOffset.UtcNow;
        var expressionDescription = CronExpressionFormatter.Format(schedule.Expression, options ?? new CronDescriptorOptions());
        var timeZoneDescription = DescriptorTextProvider.Format("Schedule_TimeZone", language, schedule.TimeZone.Id);

        var nextOccurrence = schedule.GetNextOccurrence(referenceUtc, referenceUtc.AddDays(365));
        var previousOccurrence = schedule.GetPreviousOccurrence(referenceUtc.AddDays(-365), referenceUtc);

        var nextDescription = nextOccurrence.HasValue
            ? DescriptorTextProvider.Format("Schedule_NextOccurrence", language, ToRelativeText(nextOccurrence.Value - referenceUtc, language))
            : DescriptorTextProvider.Get("Schedule_NoNextOccurrence", language);

        var previousDescription = previousOccurrence.HasValue
            ? DescriptorTextProvider.Format("Schedule_PreviousOccurrence", language, ToRelativeText(previousOccurrence.Value - referenceUtc, language))
            : DescriptorTextProvider.Get("Schedule_NoPreviousOccurrence", language);

        return $"{expressionDescription}. {timeZoneDescription}. {nextDescription}. {previousDescription}.";
    }

    /// <summary>
    /// Formats a time difference as localized relative text such as now, in N units, or N units ago.
    /// </summary>
    private static string ToRelativeText(TimeSpan difference, string language)
    {
        if (difference == TimeSpan.Zero)
        {
            return DescriptorTextProvider.Get("Relative_Now", language);
        }

        var abs = difference.Duration();
        var unitValue = abs.TotalDays >= 1
            ? (long)Math.Floor(abs.TotalDays)
            : abs.TotalHours >= 1
                ? (long)Math.Floor(abs.TotalHours)
                : abs.TotalMinutes >= 1
                    ? (long)Math.Floor(abs.TotalMinutes)
                    : Math.Max((long)Math.Floor(abs.TotalSeconds), 1);

        var unitTextKey = abs.TotalDays >= 1
            ? "Relative_Days"
            : abs.TotalHours >= 1
                ? "Relative_Hours"
                : abs.TotalMinutes >= 1
                    ? "Relative_Minutes"
                    : "Relative_Seconds";

        var unitText = DescriptorTextProvider.Format(unitTextKey, language, unitValue);

        return difference > TimeSpan.Zero
            ? DescriptorTextProvider.Format("Relative_In", language, unitText)
            : DescriptorTextProvider.Format("Relative_Ago", language, unitText);
    }
}
