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

namespace Enbrea.Cron.Tests;

public static class TimeZoneHelper
{
    /// <summary>
    /// Resolves a time zone by first attempting to find it using the IANA identifier, and if that fails, 
    /// falling back to the Windows identifier.
    /// </summary>
    /// <param name="ianaId">The IANA time zone identifier.</param>
    /// <param name="windowsId">The Windows time zone identifier.</param>
    /// <returns>The resolved <see cref="TimeZoneInfo"/>.</returns>
    public static TimeZoneInfo ResolveTimeZone(string ianaId, string windowsId)
    {
        if (string.Equals(ianaId, "UTC", StringComparison.OrdinalIgnoreCase))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(ianaId);
        }
        catch (TimeZoneNotFoundException)
        {
        }
        catch (InvalidTimeZoneException)
        {
        }

        return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
    }
}
