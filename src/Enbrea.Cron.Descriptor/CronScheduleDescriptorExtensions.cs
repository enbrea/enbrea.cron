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
/// Provides extension methods for converting <see cref="CronSchedule"/> instances into human-readable descriptions.    
/// </summary>
public static class CronScheduleDescriptorExtensions
{
    /// <summary>
    /// Formats a human-readable description of the specified cron schedule in the requested language.  
    /// </summary>
    /// <param name="schedule">The cron schedule to describe.</param>
    /// <param name="language">The language to use for the description.</param>
    /// <returns>A human-readable description of the cron schedule.</returns>
    public static string Describe(this CronSchedule schedule, string language)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        return CronScheduleFormatter.Format(schedule, new CronDescriptorOptions { Language = language });
    }

    /// <summary>
    /// Generates a human-readable description of a cron schedule.
    /// </summary>
    /// <param name="schedule">The cron schedule to describe.</param>
    /// <param name="options">Options that control how the schedule description is formatted. If <c>null</c>, 
    /// default options are used.</param>
    /// <returns>A formatted text description of the specified cron schedule.</returns>
    public static string Describe(this CronSchedule schedule, CronDescriptorOptions options = null)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        return CronScheduleFormatter.Format(schedule, options ?? new CronDescriptorOptions());
    }
}
