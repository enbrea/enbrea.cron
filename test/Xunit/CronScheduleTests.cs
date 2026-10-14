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

public sealed class CronScheduleTests
{
    [Fact]
    public void Constructor_WhenArgumentsValid_StoresExpressionAndTimeZone()
    {
        var expression = CronExpression.Parse("0 30 8 ? * MON-FRI");
        var timeZone = TimeZoneHelper.ResolveTimeZone("Europe/Berlin", "W. Europe Standard Time");

        var schedule = new CronSchedule(expression, timeZone);

        Assert.Same(expression, schedule.Expression);
        Assert.Same(timeZone, schedule.TimeZone);
    }

    [Fact]
    public void Constructor_WhenExpressionIsNull_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new CronSchedule(null, TimeZoneInfo.Utc));
        Assert.Equal("expression", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenTimeZoneIsNull_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new CronSchedule(CronExpression.Parse("* * * * *"), null));
        Assert.Equal("timeZone", exception.ParamName);
    }

    [Fact]
    public void DomDowSemantics_WhenBothAreAny_EveryDayMatches()
    {
        var schedule = new CronSchedule(CronExpression.Parse("0 0 * * *"), TimeZoneInfo.Utc);

        Assert.True(schedule.IsMatch(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero)));
        Assert.True(schedule.IsMatch(new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void DomDowSemantics_WhenBothRestricted_UsesOrSemantics()
    {
        var schedule = new CronSchedule(CronExpression.Parse("0 0 10 * MON"), TimeZoneInfo.Utc);

        Assert.True(schedule.IsMatch(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero)));
        Assert.True(schedule.IsMatch(new DateTimeOffset(2026, 8, 17, 0, 0, 0, TimeSpan.Zero)));
        Assert.False(schedule.IsMatch(new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void DomDowSemantics_WhenDayOfMonthIsAnyAndDayOfWeekRestricted_UsesDayOfWeek()
    {
        var schedule = new CronSchedule(CronExpression.Parse("0 0 * * MON"), TimeZoneInfo.Utc);

        Assert.True(schedule.IsMatch(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero)));
        Assert.False(schedule.IsMatch(new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void DomDowSemantics_WhenDayOfMonthIsQuestion_UsesOnlyDayOfWeek()
    {
        var schedule = new CronSchedule(CronExpression.Parse("0 0 ? * MON"), TimeZoneInfo.Utc);

        Assert.True(schedule.IsMatch(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero)));
        Assert.False(schedule.IsMatch(new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void DomDowSemantics_WhenDayOfWeekIsAnyAndDayOfMonthRestricted_UsesDayOfMonth()
    {
        var schedule = new CronSchedule(CronExpression.Parse("0 0 10 * *"), TimeZoneInfo.Utc);

        Assert.True(schedule.IsMatch(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero)));
        Assert.False(schedule.IsMatch(new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void DomDowSemantics_WhenDayOfWeekIsQuestion_UsesOnlyDayOfMonth()
    {
        var schedule = new CronSchedule(CronExpression.Parse("0 0 10 * ?"), TimeZoneInfo.Utc);

        Assert.True(schedule.IsMatch(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero)));
        Assert.False(schedule.IsMatch(new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void DstGapPolicy_WhenLocalTimeIsInvalid_SkipsOccurrenceWithoutReplacement()
    {
        var berlin = TimeZoneHelper.ResolveTimeZone("Europe/Berlin", "W. Europe Standard Time");
        var schedule = new CronSchedule(CronExpression.Parse("0 30 2 * * ?"), berlin);

        var occurrences = schedule.GetOccurrences(
            new DateTimeOffset(2026, 3, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 31, 0, 0, 0, TimeSpan.Zero),
            CronOccurrenceOrder.Ascending,
            TestContext.Current.CancellationToken).ToArray();

        Assert.Equal(
            [
                new DateTimeOffset(2026, 3, 28, 1, 30, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 3, 30, 0, 30, 0, TimeSpan.Zero)
            ],
            occurrences);

        Assert.All(occurrences, occurrence => Assert.True(schedule.IsMatch(occurrence)));
    }

    [Fact]
    public void DstGapPolicy_WhenSearchingAcrossGap_PreservesCurrentBehavior()
    {
        var berlin = TimeZoneHelper.ResolveTimeZone("Europe/Berlin", "W. Europe Standard Time");
        var schedule = new CronSchedule(CronExpression.Parse("0 30 2 * * ?"), berlin);

        var next = schedule.GetNextOccurrence(
            new DateTimeOffset(2026, 3, 29, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 31, 0, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken);

        var previous = schedule.GetPreviousOccurrence(
            new DateTimeOffset(2026, 3, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken);

        Assert.Equal(new DateTimeOffset(2026, 3, 30, 0, 30, 0, TimeSpan.Zero), next);
        Assert.Equal(new DateTimeOffset(2026, 3, 28, 1, 30, 0, TimeSpan.Zero), previous);
    }

    [Fact]
    public void DstOverlapPolicy_WhenDescendingStartsInsideLaterOccurrence_ReturnsClosestEarlierSlotsWithoutDuplicates()
    {
        var berlin = TimeZoneHelper.ResolveTimeZone("Europe/Berlin", "W. Europe Standard Time");
        var schedule = new CronSchedule(CronExpression.Parse("0 * 2 25 10 ?"), berlin);
        var from = new DateTimeOffset(2026, 10, 24, 22, 0, 0, TimeSpan.Zero);
        var untilExclusive = new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.Zero);

        var descending = schedule.GetOccurrences(from, untilExclusive, CronOccurrenceOrder.Descending, TestContext.Current.CancellationToken).ToArray();

        Assert.NotEmpty(descending);
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 0, 59, 0, TimeSpan.Zero), descending[0]);

        for (var index = 1; index < descending.Length; index++)
        {
            Assert.True(descending[index] < descending[index - 1]);
        }

        Assert.Equal(descending.Length, descending.Distinct().Count());
    }

    [Fact]
    public void DstOverlapPolicy_WhenTimeIsAmbiguous_UsesEarlierOccurrenceWithoutDuplicates()
    {
        var berlin = TimeZoneHelper.ResolveTimeZone("Europe/Berlin", "W. Europe Standard Time");
        var schedule = new CronSchedule(CronExpression.Parse("0 30 2 25 10 ?"), berlin);

        var ascending = schedule.GetOccurrences(
            new DateTimeOffset(2026, 10, 24, 22, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 25, 4, 0, 0, TimeSpan.Zero),
            CronOccurrenceOrder.Ascending,
            TestContext.Current.CancellationToken).ToArray();

        Assert.Equal([new DateTimeOffset(2026, 10, 25, 0, 30, 0, TimeSpan.Zero)], ascending);
    }

    [Fact]
    public void GetOccurrences_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        var schedule = new CronSchedule(CronExpression.Parse("0 0 31 2 ?"), TimeZoneInfo.Utc);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            schedule.GetOccurrences(
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2035, 1, 1, 0, 0, 0, TimeSpan.Zero),
                CronOccurrenceOrder.Ascending,
                cancellation.Token).ToArray());
    }

    [Fact]
    public void GetOccurrences_WhenIntervalCrossesSupportedRangeBoundaries_ReturnsOnlySupportedYears()
    {
        var schedule = new CronSchedule(CronExpression.Parse("0 0 0 1 1 ?"), TimeZoneInfo.Utc);

        var ascending = schedule.GetOccurrences(
            new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(CronYearBounds.MinYear + 1, 1, 1, 0, 0, 0, TimeSpan.Zero),
            CronOccurrenceOrder.Ascending,
            TestContext.Current.CancellationToken).ToArray();

        var descending = schedule.GetOccurrences(
            new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2300, 1, 1, 0, 0, 0, TimeSpan.Zero),
            CronOccurrenceOrder.Descending,
            TestContext.Current.CancellationToken).ToArray();

        Assert.Equal([new DateTimeOffset(CronYearBounds.MinYear, 1, 1, 0, 0, 0, TimeSpan.Zero)], ascending);
        Assert.Equal(new DateTimeOffset(CronYearBounds.MaxYear, 1, 1, 0, 0, 0, TimeSpan.Zero), descending.First());
        Assert.Equal(new DateTimeOffset(CronYearBounds.MinYear, 1, 1, 0, 0, 0, TimeSpan.Zero), descending.Last());
    }

    [Theory]
    [InlineData(1900, 1901)]
    [InlineData(2300, 2301)]
    public void GetOccurrences_WhenIntervalIsOutsideSupportedRange_ReturnsNoOccurrences(int fromYear, int untilYear)
    {
        var schedule = new CronSchedule(CronExpression.Parse("0 0 0 1 1 ?"), TimeZoneInfo.Utc);
        var from = new DateTimeOffset(fromYear, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var until = new DateTimeOffset(untilYear, 1, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.Null(schedule.GetNextOccurrence(from, until, TestContext.Current.CancellationToken));
        Assert.Null(schedule.GetPreviousOccurrence(from, until, TestContext.Current.CancellationToken));
        Assert.Empty(schedule.GetOccurrences(from, until, CronOccurrenceOrder.Ascending, TestContext.Current.CancellationToken));
        Assert.Empty(schedule.GetOccurrences(from, until, CronOccurrenceOrder.Descending, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(CronOccurrenceOrder.Ascending)]
    [InlineData(CronOccurrenceOrder.Descending)]
    public void GetOccurrences_WhenUntilPrecedesFrom_ThrowsArgumentException(CronOccurrenceOrder order)
    {
        var schedule = new CronSchedule(CronExpression.Parse("* * * * *"), TimeZoneInfo.Utc);
        var from = new DateTimeOffset(2026, 8, 6, 12, 31, 0, TimeSpan.Zero);
        var until = new DateTimeOffset(2026, 8, 6, 12, 30, 0, TimeSpan.Zero);

        var exception = Assert.Throws<ArgumentException>(() =>
            schedule.GetOccurrences(from, until, order, TestContext.Current.CancellationToken).ToArray());

        Assert.Equal("untilExclusive", exception.ParamName);
    }

    [Fact]
    public void GetOccurrences_WithExplicitYearStepRange_UsesActualMatchingYears()
    {
        var schedule = new CronSchedule(CronExpression.Parse("0 0 0 1 1 ? 2026-2030/3"), TimeZoneInfo.Utc);
        var occurrences = schedule.GetOccurrences(
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2031, 1, 1, 0, 0, 0, TimeSpan.Zero),
            CronOccurrenceOrder.Ascending,
            TestContext.Current.CancellationToken).ToArray();

        Assert.Equal(
            [
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2029, 1, 1, 0, 0, 0, TimeSpan.Zero)
            ],
            occurrences);
    }

    [Fact]
    public void ImpossibleDate_WhenEnumeratingAscendingOrDescending_ReturnsNoOccurrences()
    {
        var schedule = new CronSchedule(CronExpression.Parse("0 0 31 2 ?"), TimeZoneInfo.Utc);
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var until = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.Empty(schedule.GetOccurrences(from, until, CronOccurrenceOrder.Ascending, TestContext.Current.CancellationToken));
        Assert.Empty(schedule.GetOccurrences(from, until, CronOccurrenceOrder.Descending, TestContext.Current.CancellationToken));
        Assert.Null(schedule.GetNextOccurrence(from, until, TestContext.Current.CancellationToken));
        Assert.Null(schedule.GetPreviousOccurrence(from, until, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void IsMatch_WhenEquivalentInstantsUseDifferentOriginalOffsets_BehavesIdentically()
    {
        var schedule = new CronSchedule(CronExpression.Parse("0 30 8 * * ?"), TimeZoneInfo.Utc);
        var first = new DateTimeOffset(2026, 1, 7, 8, 30, 0, TimeSpan.Zero);
        var second = new DateTimeOffset(2026, 1, 7, 9, 30, 0, TimeSpan.FromHours(1));

        Assert.Equal(first.ToUniversalTime(), second.ToUniversalTime());
        Assert.Equal(schedule.IsMatch(first), schedule.IsMatch(second));
    }

    [Fact]
    public void IsMatch_WhenInstantIsLaterAmbiguousOccurrence_ReturnsFalseForUnselectedOverlapOccurrence()
    {
        var berlin = TimeZoneHelper.ResolveTimeZone("Europe/Berlin", "W. Europe Standard Time");
        var schedule = new CronSchedule(CronExpression.Parse("0 30 2 25 10 ?"), berlin);

        var earlier = new DateTimeOffset(2026, 10, 25, 0, 30, 0, TimeSpan.Zero);
        var later = new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.Zero);

        Assert.True(schedule.IsMatch(earlier));
        Assert.False(schedule.IsMatch(later));
    }

    [Fact]
    public void IsMatch_WhenInterpretingSameInstantInDifferentTimeZones_CanDiffer()
    {
        var expression = CronExpression.Parse("0 30 8 * * ?");
        var utcSchedule = new CronSchedule(expression, TimeZoneInfo.Utc);
        var berlinSchedule = new CronSchedule(expression, TimeZoneHelper.ResolveTimeZone("Europe/Berlin", "W. Europe Standard Time"));
        var instant = new DateTimeOffset(2026, 1, 7, 8, 30, 0, TimeSpan.Zero);

        Assert.True(utcSchedule.IsMatch(instant));
        Assert.False(berlinSchedule.IsMatch(instant));
    }

    [Fact]
    public void Parse_WhenInputIsValid_ReturnsScheduleWithExpressionAndTimeZone()
    {
        var timeZone = TimeZoneHelper.ResolveTimeZone("Europe/Berlin", "W. Europe Standard Time");

        var schedule = CronSchedule.Parse("0 30 8 ? * MON-FRI", timeZone);

        Assert.Equal("0 30 8 ? * 1-5", schedule.Expression.ToString());
        Assert.Same(timeZone, schedule.TimeZone);
    }

    [Theory]
    [InlineData("0 0 L * ?", 2026, 2, 28)]
    [InlineData("0 0 LW * ?", 2026, 2, 27)]
    [InlineData("0 0 15W * ?", 2026, 8, 14)]
    [InlineData("0 0 ? * 5L", 2026, 8, 28)]
    [InlineData("0 0 ? * FRIL", 2026, 8, 28)]
    [InlineData("0 0 ? * MON#2", 2026, 8, 10)]
    public void SpecialDaySyntax_WhenEnumeratingOccurrences_MatchesExpectedDate(string expressionText, int year, int month, int day)
    {
        var schedule = new CronSchedule(CronExpression.Parse(expressionText), TimeZoneInfo.Utc);
        var from = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var until = from.AddMonths(1);

        var occurrence = schedule.GetNextOccurrence(from, until, TestContext.Current.CancellationToken);

        Assert.Equal(new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero), occurrence);
    }

    [Theory]
    [InlineData("0 0 ? * 0")]
    [InlineData("0 0 ? * 7")]
    [InlineData("0 0 ? * SUN")]
    public void SundayAliases_WhenEnumeratingOccurrences_ProduceSameSequence(string expressionText)
    {
        var schedule = new CronSchedule(CronExpression.Parse(expressionText), TimeZoneInfo.Utc);
        var occurrences = schedule.GetOccurrences(
            new DateTimeOffset(2026, 8, 6, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 17, 0, 0, 0, TimeSpan.Zero),
            CronOccurrenceOrder.Ascending,
            TestContext.Current.CancellationToken).ToArray();

        Assert.Equal(
            [
                new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 16, 0, 0, 0, TimeSpan.Zero)
            ],
            occurrences);
    }

    [Fact]
    public void TryParse_EmptyTimeZoneId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            CronSchedule.TryParse("0 9 * * *", string.Empty, out _));

        Assert.Equal("timeZoneId", exception.ParamName);
    }

    [Fact]
    public void TryParse_InvalidExpression_ReturnsFalse()
    {
        var result = CronSchedule.TryParse("invalid", TimeZoneInfo.Utc, out var schedule);

        Assert.False(result);
        Assert.Null(schedule);
    }

    [Fact]
    public void TryParse_NullTimeZoneId_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            CronSchedule.TryParse("0 9 * * *", (string)null!, out _));

        Assert.Equal("timeZoneId", exception.ParamName);
    }
    [Fact]
    public void TryParse_UnknownTimeZoneId_ReturnsFalse()
    {
        var result = CronSchedule.TryParse("0 9 * * *", "Invalid/TimeZone", out var schedule);

        Assert.False(result);
        Assert.Null(schedule);
    }

    [Fact]
    public void TryParse_ValidExpression_ReturnsTrue()
    {
        var timeZone = TimeZoneInfo.Utc;

        var result = CronSchedule.TryParse("0 9 * * MON-FRI", timeZone, out var schedule);

        Assert.True(result);
        Assert.NotNull(schedule);
        Assert.Equal(timeZone, schedule.TimeZone);
    }

    [Fact]
    public void TryParse_ValidTimeZoneId_ReturnsTrue()
    {
        var result = CronSchedule.TryParse("0 9 * * *", "UTC", out var schedule);

        Assert.True(result);
        Assert.NotNull(schedule);
    }

    [Fact]
    public void TryParse_WhenTimeZoneIsNull_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => CronSchedule.TryParse("* * * * *", (TimeZoneInfo)null!, out _));
        Assert.Equal("timeZone", exception.ParamName);
    }

    [Fact]
    public void UtcSchedule_WhenEnumeratingOccurrences_PreservesUtcBehavior()
    {
        var schedule = new CronSchedule(CronExpression.Parse("* * * * *"), TimeZoneInfo.Utc);
        var from = new DateTimeOffset(2026, 8, 6, 12, 30, 0, TimeSpan.Zero);
        var until = new DateTimeOffset(2026, 8, 6, 12, 33, 0, TimeSpan.Zero);

        var ascending = schedule.GetOccurrences(from, until, CronOccurrenceOrder.Ascending, TestContext.Current.CancellationToken).ToArray();
        var descending = schedule.GetOccurrences(from, until, CronOccurrenceOrder.Descending, TestContext.Current.CancellationToken).ToArray();

        Assert.Equal(
            [
                new DateTimeOffset(2026, 8, 6, 12, 30, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 6, 12, 31, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 6, 12, 32, 0, TimeSpan.Zero)
            ],
            ascending);

        Assert.Equal(ascending.Reverse().ToArray(), descending);
    }

    [Fact]
    public void WeekdaySteps_WhenEnumeratingOccurrences_ReturnExpectedDays()
    {
        var schedule = new CronSchedule(CronExpression.Parse("0 0 ? * MON-FRI/2"), TimeZoneInfo.Utc);
        var occurrences = schedule.GetOccurrences(
            new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 17, 0, 0, 0, TimeSpan.Zero),
            CronOccurrenceOrder.Ascending,
            TestContext.Current.CancellationToken).ToArray();

        Assert.Equal(
            [
                new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 12, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 14, 0, 0, 0, TimeSpan.Zero)
            ],
            occurrences);
    }

    [Fact]
    public void WrappedWeekdayRange_WhenEnumeratingOccurrences_ReturnsWrappedDays()
    {
        var schedule = new CronSchedule(CronExpression.Parse("0 0 ? * FRI-MON"), TimeZoneInfo.Utc);
        var occurrences = schedule.GetOccurrences(
            new DateTimeOffset(2026, 8, 6, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 12, 0, 0, 0, TimeSpan.Zero),
            CronOccurrenceOrder.Ascending,
            TestContext.Current.CancellationToken).ToArray();

        Assert.Equal(
            [
                new DateTimeOffset(2026, 8, 7, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 8, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero)
            ],
            occurrences);
    }
}
