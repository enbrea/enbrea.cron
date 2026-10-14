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

using Xunit;

namespace Enbrea.Cron.Tests;

public sealed class CronTimeZoneMapperTests
{
    [Fact]
    public void TryResolveLocalTimeToUtc_WhenGapIsThirtyMinutes_ReturnsFalseForInvalidLocalTime()
    {
        var customZone = CreateThirtyMinuteGapTimeZone();
        var mapper = new CronTimeZoneMapper(customZone);
        var invalidLocal = new DateTime(2026, 3, 8, 2, 15, 0, DateTimeKind.Unspecified);

        var success = mapper.TryResolveLocalTimeToUtc(invalidLocal, out _);

        Assert.False(success);
    }

    [Fact]
    public void TryResolveLocalTimeToUtc_WhenLocalTimeIsAmbiguous_UsesEarlierUtcOccurrencePolicy()
    {
        var berlin = TimeZoneHelper.ResolveTimeZone("Europe/Berlin", "W. Europe Standard Time");
        var mapper = new CronTimeZoneMapper(berlin);
        var ambiguousLocal = new DateTime(2026, 10, 25, 2, 30, 0, DateTimeKind.Unspecified);

        var success = mapper.TryResolveLocalTimeToUtc(ambiguousLocal, out var utcTime);

        Assert.True(success);
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 0, 30, 0, TimeSpan.Zero), utcTime);
    }

    [Fact]
    public void TryResolveLocalTimeToUtc_WhenLocalTimeIsInvalid_ReturnsFalse()
    {
        var berlin = TimeZoneHelper.ResolveTimeZone("Europe/Berlin", "W. Europe Standard Time");
        var mapper = new CronTimeZoneMapper(berlin);
        var invalidLocal = new DateTime(2026, 3, 29, 2, 30, 0, DateTimeKind.Unspecified);

        var success = mapper.TryResolveLocalTimeToUtc(invalidLocal, out _);

        Assert.False(success);
    }

    private static TimeZoneInfo CreateThirtyMinuteGapTimeZone()
    {
        var daylightDelta = TimeSpan.FromMinutes(30);

        var transitionToDaylight = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 2, 0, 0),
            3,
            2,
            DayOfWeek.Sunday);

        var transitionToStandard = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 2, 0, 0),
            11,
            1,
            DayOfWeek.Sunday);

        var adjustmentRule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2000, 1, 1),
            new DateTime(2100, 12, 31),
            daylightDelta,
            transitionToDaylight,
            transitionToStandard);

        return TimeZoneInfo.CreateCustomTimeZone(
            "Test/ThirtyMinuteGap",
            TimeSpan.Zero,
            "Test Thirty Minute Gap",
            "Test Thirty Minute Gap",
            "Test Thirty Minute Gap DST",
            [adjustmentRule]);
    }
}

