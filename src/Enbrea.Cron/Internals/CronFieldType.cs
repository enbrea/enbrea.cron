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
/// Specifies the individual fields of a CRON expression.
/// </summary>
internal enum CronFieldType
{
    Seconds,
    Minutes,
    Hours,
    DayOfMonth,
    Month,
    DayOfWeek,
    Year
}
