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

public sealed class CronExpressionTests
{
    [Fact]
    public void GetFirstAndLastMatchingValue_WhenUsingList_ReturnExpectedValues()
    {
        var expression = CronExpression.Parse("30,10,20 * * * *");
        var field = expression.Minutes;

        Assert.Equal(10, field.GetFirstMatchingValue(0, 59));
        Assert.Equal(30, field.GetLastMatchingValue(0, 59));
    }

    [Theory]
    [InlineData("0 0 * */2 *", new[] { 1, 3, 5, 7, 9, 11 })]
    [InlineData("0 0 * */3 *", new[] { 1, 4, 7, 10 })]
    public void Matches_WhenUsingMonthWildcardStep_AnchorsAtMonthMinimum(string expressionText, int[] expected)
    {
        var expression = CronExpression.Parse(expressionText);

        Assert.Equal(expected, GetMatchingValues(expression.Months, 1, 12));
    }

    [Theory]
    [InlineData("*", 0, 59, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59 })]
    [InlineData("15", 0, 59, new[] { 15 })]
    [InlineData("10-20", 0, 59, new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 })]
    [InlineData("*/15", 0, 59, new[] { 0, 15, 30, 45 })]
    [InlineData("10/15", 0, 59, new[] { 10, 25, 40, 55 })]
    [InlineData("10-30/5", 0, 59, new[] { 10, 15, 20, 25, 30 })]
    [InlineData("5,10,20", 0, 59, new[] { 5, 10, 20 })]
    [InlineData("5,10-20/5,40", 0, 59, new[] { 5, 10, 15, 20, 40 })]
    public void Matches_WhenUsingOrdinaryNumericForms_ReturnsExpectedValues(string minuteField, int min, int max, int[] expected)
    {
        var expression = CronExpression.Parse($"{minuteField} * * * *");

        Assert.Equal(expected, GetMatchingValues(expression.Minutes, min, max));
    }

    [Fact]
    public void Matches_WhenUsingYearWildcardStep_AnchorsAtCronYearBoundsMinimum()
    {
        var expression = CronExpression.Parse("0 0 0 1 1 ? */5");

        var expected = new List<int>();
        for (var year = CronYearBounds.MinYear; year <= CronYearBounds.MaxYear; year += 5)
        {
            expected.Add(year);
        }

        Assert.Equal(expected, GetMatchingValues(expression.Years, CronYearBounds.MinYear, CronYearBounds.MaxYear));
    }

    [Fact]
    public void TryGetNextAndPreviousMatchingValue_WhenUsingStepFields_ReturnExpectedValues()
    {
        var wildcardStep = CronExpression.Parse("*/15 * * * *").Minutes;

        Assert.True(wildcardStep.TryGetNextMatchingValue(1, 0, 59, out var nextWildcard));
        Assert.Equal(15, nextWildcard);

        Assert.True(wildcardStep.TryGetPreviousMatchingValue(44, 0, 59, out var previousWildcard));
        Assert.Equal(30, previousWildcard);

        var rangeStep = CronExpression.Parse("10-50/10 * * * *").Minutes;

        Assert.True(rangeStep.TryGetNextMatchingValue(21, 0, 59, out var nextRange));
        Assert.Equal(30, nextRange);

        Assert.True(rangeStep.TryGetPreviousMatchingValue(49, 0, 59, out var previousRange));
        Assert.Equal(40, previousRange);
    }

    [Fact]
    public void TryGetNextMatchingValue_WhenUsingList_ReturnsExpectedInclusiveResults()
    {
        var expression = CronExpression.Parse("10,20,30 * * * *");
        var field = expression.Minutes;

        Assert.True(field.TryGetNextMatchingValue(5, 0, 59, out var valueAt5));
        Assert.Equal(10, valueAt5);

        Assert.True(field.TryGetNextMatchingValue(10, 0, 59, out var valueAt10));
        Assert.Equal(10, valueAt10);

        Assert.True(field.TryGetNextMatchingValue(11, 0, 59, out var valueAt11));
        Assert.Equal(20, valueAt11);

        Assert.True(field.TryGetNextMatchingValue(30, 0, 59, out var valueAt30));
        Assert.Equal(30, valueAt30);

        Assert.False(field.TryGetNextMatchingValue(31, 0, 59, out _));
    }

    [Fact]
    public void TryGetPreviousMatchingValue_WhenUsingList_ReturnsExpectedInclusiveResults()
    {
        var expression = CronExpression.Parse("10,20,30 * * * *");
        var field = expression.Minutes;

        Assert.True(field.TryGetPreviousMatchingValue(35, 0, 59, out var valueAt35));
        Assert.Equal(30, valueAt35);

        Assert.True(field.TryGetPreviousMatchingValue(30, 0, 59, out var valueAt30));
        Assert.Equal(30, valueAt30);

        Assert.True(field.TryGetPreviousMatchingValue(29, 0, 59, out var valueAt29));
        Assert.Equal(20, valueAt29);

        Assert.True(field.TryGetPreviousMatchingValue(10, 0, 59, out var valueAt10));
        Assert.Equal(10, valueAt10);

        Assert.False(field.TryGetPreviousMatchingValue(9, 0, 59, out _));
    }

    /// <summary>
    /// Gets all matching values for a given CronField within the specified minimum and maximum range.
    /// </summary>
    private static int[] GetMatchingValues(CronField field, int minimum, int maximum)
    {
        var values = new List<int>();

        for (var value = minimum; value <= maximum; value++)
        {
            if (field.Matches(value, minimum, maximum))
            {
                values.Add(value);
            }
        }

        return [.. values];
    }
}
