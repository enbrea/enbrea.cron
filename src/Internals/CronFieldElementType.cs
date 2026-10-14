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
/// Identifies a cron field element type.
/// </summary>
internal enum CronFieldElementType
{
    Any,
    Value,
    Range,
    Step,
    Question,
    Last,
    LastWeekday,
    NearestWeekday,
    LastDayOfWeek,
    Hash
}
