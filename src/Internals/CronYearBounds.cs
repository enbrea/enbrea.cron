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
/// Defines the inclusive local calendar year range supported by Enbrea.Cron for parsing,
/// matching, and occurrence calculation.
/// </summary>
internal static class CronYearBounds
{
    /// <summary>
    /// Defines the minimum supported local calendar year.
    /// </summary>
    internal const int MinYear = 1970;

    /// <summary>
    /// Defines the maximum supported local calendar year.
    /// </summary>
    internal const int MaxYear = 2199;
}