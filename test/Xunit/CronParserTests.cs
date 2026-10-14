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

using System.Globalization;
using Xunit;

namespace Enbrea.Cron.Tests;

public sealed class CronParserTests
{
    [Fact]
    public void GetSlotUtc_WhenInstantsAreEquivalent_ReturnsSameUtcSlot()
    {
        var expression = CronExpression.Parse("* * * * *");
        var first = new DateTimeOffset(2026, 8, 6, 12, 0, 0, TimeSpan.FromHours(2));
        var second = new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero);

        Assert.Equal(first.ToUniversalTime(), second.ToUniversalTime());
        Assert.Equal(expression.GetSlotUtc(first), expression.GetSlotUtc(second));
    }

    [Fact]
    public void GetSlotUtc_WhenUsingFiveFieldExpression_UsesMinutePrecision()
    {
        var expression = CronExpression.Parse("* * * * *");
        var slot = expression.GetSlotUtc(new DateTimeOffset(2026, 8, 6, 12, 0, 59, 900, TimeSpan.FromHours(2)));

        Assert.Equal(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero), slot);
        Assert.Equal(TimeSpan.Zero, slot.Offset);
    }

    [Fact]
    public void GetSlotUtc_WhenUsingSevenFieldExpression_UsesSecondPrecision()
    {
        var expression = CronExpression.Parse("0 * * * * * 2026");
        var slot = expression.GetSlotUtc(new DateTimeOffset(2026, 8, 6, 12, 0, 30, 999, TimeSpan.FromHours(2)));

        Assert.Equal(new DateTimeOffset(2026, 8, 6, 10, 0, 30, TimeSpan.Zero), slot);
    }

    [Fact]
    public void GetSlotUtc_WhenUsingSixFieldExpression_UsesSecondPrecision()
    {
        var expression = CronExpression.Parse("0 * * * * *");
        var slot = expression.GetSlotUtc(new DateTimeOffset(2026, 8, 6, 12, 0, 30, 999, TimeSpan.FromHours(2)));

        Assert.Equal(new DateTimeOffset(2026, 8, 6, 10, 0, 30, TimeSpan.Zero), slot);
        Assert.Equal(TimeSpan.Zero, slot.Offset);
    }

    [Fact]
    public void Parse_WhenDayOfMonthLastTokenUsed_ReturnsLastElement()
    {
        var expression = CronExpression.Parse("0 0 L * ?");

        Assert.Equal(CronFieldElementType.Last, expression.DayOfMonth.Items[0].Type);
        Assert.Equal(CronFieldElementType.Question, expression.DayOfWeek.Items[0].Type);
    }

    [Fact]
    public void Parse_WhenDayOfMonthLastWeekdayTokenUsed_ReturnsLastWeekdayElement()
    {
        var expression = CronExpression.Parse("0 0 LW * ?");

        Assert.Equal(CronFieldElementType.LastWeekday, expression.DayOfMonth.Items[0].Type);
    }

    [Fact]
    public void Parse_WhenDayOfMonthNearestWeekdayTokenUsed_ReturnsNearestWeekdayElement()
    {
        var expression = CronExpression.Parse("0 0 15W * ?");
        var item = expression.DayOfMonth.Items[0];

        Assert.Equal(CronFieldElementType.NearestWeekday, item.Type);
        Assert.Equal(15, item.Value);
    }

    [Fact]
    public void Parse_WhenDayOfWeekHashTokenUsed_ReturnsHashElement()
    {
        var expression = CronExpression.Parse("0 0 ? * 2#3");
        var item = expression.DayOfWeek.Items[0];

        Assert.Equal(CronFieldElementType.Hash, item.Type);
        Assert.Equal(2, item.Value);
        Assert.Equal(3, item.Occurrence);
    }

    [Fact]
    public void Parse_WhenDayOfWeekIsSeven_AcceptsSundayAlias()
    {
        var expression = CronExpression.Parse("0 0 * * 7");

        Assert.Equal(7, expression.DayOfWeek.Items[0].Value);
    }

    [Fact]
    public void Parse_WhenDayOfWeekLastTokenUsed_ReturnsLastDayOfWeekElement()
    {
        var expression = CronExpression.Parse("0 0 ? * 5L");
        var item = expression.DayOfWeek.Items[0];

        Assert.Equal(CronFieldElementType.LastDayOfWeek, item.Type);
        Assert.Equal(5, item.Value);
    }

    [Theory]
    [InlineData("0 0 ? * MON#2")] 
    [InlineData("0 0 ? * FRI-L")] 
    public void Parse_WhenDayOfWeekNameFormsUsed_ValidatesSupportedSyntax(string expression)
    {
        if (expression == "0 0 ? * MON#2")
        {
            var parsed = CronExpression.Parse(expression);
            Assert.Equal(CronFieldElementType.Hash, parsed.DayOfWeek.Items[0].Type);
            return;
        }

        Assert.Throws<FormatException>(() => CronExpression.Parse(expression));
    }
    [Fact]
    public void Parse_WhenDayOfWeekRangeWrapsAround_ReturnsWrapAroundRange()
    {
        var expression = CronExpression.Parse("0 0 ? * FRI-MON");
        var item = expression.DayOfWeek.Items[0];

        Assert.Equal(CronFieldElementType.Range, item.Type);
        Assert.Equal(5, item.Start);
        Assert.Equal(1, item.End);
    }

    [Theory]
    [InlineData(CronYearBounds.MinYear - 1, CronYearBounds.MinYear, false)]
    [InlineData(CronYearBounds.MinYear, CronYearBounds.MaxYear, true)]
    [InlineData(CronYearBounds.MaxYear, CronYearBounds.MaxYear + 1, false)]
    public void Parse_WhenExplicitYearListIsAtBounds_ValidatesAgainstCronYearBounds(int yearA, int yearB, bool shouldSucceed)
    {
        var expression = $"0 0 0 1 1 ? {yearA},{yearB}";

        if (shouldSucceed)
        {
            var parsed = CronExpression.Parse(expression);
            Assert.Equal(expression, parsed.ToString());
            return;
        }

        Assert.Throws<FormatException>(() => CronExpression.Parse(expression));
    }

    [Theory]
    [InlineData(CronYearBounds.MinYear - 1, CronYearBounds.MinYear, false)]
    [InlineData(CronYearBounds.MinYear, CronYearBounds.MinYear + 1, true)]
    [InlineData(CronYearBounds.MaxYear - 1, CronYearBounds.MaxYear, true)]
    [InlineData(CronYearBounds.MaxYear, CronYearBounds.MaxYear + 1, false)]
    public void Parse_WhenExplicitYearRangeIsAtBounds_ValidatesAgainstCronYearBounds(int startYear, int endYear, bool shouldSucceed)
    {
        var expression = $"0 0 0 1 1 ? {startYear}-{endYear}";

        if (shouldSucceed)
        {
            var parsed = CronExpression.Parse(expression);
            Assert.Equal(expression, parsed.ToString());
            return;
        }

        Assert.Throws<FormatException>(() => CronExpression.Parse(expression));
    }

    [Theory]
    [InlineData(CronYearBounds.MinYear - 1, CronYearBounds.MinYear + 1, 1, false)]
    [InlineData(CronYearBounds.MinYear, CronYearBounds.MinYear + 4, 2, true)]
    [InlineData(CronYearBounds.MaxYear - 4, CronYearBounds.MaxYear, 2, true)]
    [InlineData(CronYearBounds.MaxYear - 1, CronYearBounds.MaxYear + 1, 1, false)]
    public void Parse_WhenExplicitYearRangeStepIsAtBounds_ValidatesAgainstCronYearBounds(int startYear, int endYear, int step, bool shouldSucceed)
    {
        var expression = $"0 0 0 1 1 ? {startYear}-{endYear}/{step}";

        if (shouldSucceed)
        {
            var parsed = CronExpression.Parse(expression);
            Assert.Equal(expression, parsed.ToString());
            return;
        }

        Assert.Throws<FormatException>(() => CronExpression.Parse(expression));
    }

    [Theory]
    [InlineData(CronYearBounds.MinYear - 1, false)]
    [InlineData(CronYearBounds.MinYear, true)]
    [InlineData(CronYearBounds.MaxYear, true)]
    [InlineData(CronYearBounds.MaxYear + 1, false)]
    public void Parse_WhenExplicitYearValueIsAtBounds_ValidatesAgainstCronYearBounds(int year, bool shouldSucceed)
    {
        var expression = $"0 0 0 1 1 ? {year}";

        if (shouldSucceed)
        {
            var parsed = CronExpression.Parse(expression);
            Assert.Equal(expression, parsed.ToString());
            return;
        }

        Assert.Throws<FormatException>(() => CronExpression.Parse(expression));
    }

    [Fact]
    public void Parse_WhenExpressionContainsExtraWhitespace_NormalizesWhitespace()
    {
        var expression = CronExpression.Parse("  0   */5   *   *   *   *  ");

        Assert.Equal("0 */5 * * * *", expression.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_WhenExpressionIsEmpty_ThrowsArgumentException(string expression)
    {
        Assert.Throws<ArgumentException>(() => CronExpression.Parse(expression));
    }

    [Fact]
    public void Parse_WhenFieldCountIsInvalid_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CronExpression.Parse("* * *"));
    }

    [Fact]
    public void Parse_WhenFiveFieldExpressionProvided_ReturnsCronExpression()
    {
        var expression = CronExpression.Parse("*/15 8-18 * * MON-FRI");

        Assert.False(expression.HasSeconds);
        Assert.False(expression.HasYear);
        Assert.Equal("*/15 8-18 * * 1-5", expression.ToString());
        Assert.Equal(CronFieldElementType.Step, expression.Minutes.Items[0].Type);
        Assert.Equal(CronFieldElementType.Range, expression.Hours.Items[0].Type);
    }

    [Theory]
    [InlineData("0 0 ? * 2#0")]
    [InlineData("0 0 ? * 2#6")]
    [InlineData("0 0 ? * 2#X")]
    public void Parse_WhenHashOccurrenceIsInvalid_ThrowsFormatException(string expression)
    {
        Assert.Throws<FormatException>(() => CronExpression.Parse(expression));
    }

    [Theory]
    [InlineData("0 0 ? * 1##2")]
    [InlineData("0 0 ? * 1#2#3")]
    public void Parse_WhenHashSyntaxIsInvalid_ThrowsFormatException(string expression)
    {
        Assert.Throws<FormatException>(() => CronExpression.Parse(expression));
    }

    [Fact]
    public void Parse_WhenMonthAndDayNamesUsed_ReturnsNamedValues()
    {
        var expression = CronExpression.Parse("0 0 1 JAN MON");

        Assert.Equal(CronFieldElementType.Value, expression.Months.Items[0].Type);
        Assert.Equal(1, expression.Months.Items[0].Value);
        Assert.Equal(CronFieldElementType.Value, expression.DayOfWeek.Items[0].Type);
        Assert.Equal(1, expression.DayOfWeek.Items[0].Value);
    }

    [Theory]
    [InlineData("JAN 0 * * *")]
    [InlineData("0 JAN * * *")]
    [InlineData("0 0 JAN * *")]
    public void Parse_WhenNamedValuesUsedInWrongField_ThrowsFormatException(string expression)
    {
        Assert.Throws<FormatException>(() => CronExpression.Parse(expression));
    }

    [Theory]
    [InlineData("0 0 10-1 * *")]
    public void Parse_WhenNonDayOfWeekRangeIsDescending_ThrowsFormatException(string expression)
    {
        Assert.Throws<FormatException>(() => CronExpression.Parse(expression));
    }

    [Theory]
    [InlineData("0 0 ? * ?")]
    [InlineData("0 0 ?,1 * *")]
    [InlineData("0 0 * * ?,1")]
    [InlineData("? 0 * * *")]
    public void Parse_WhenQuestionUsageIsInvalid_ThrowsFormatException(string expression)
    {
        Assert.Throws<FormatException>(() => CronExpression.Parse(expression));
    }

    [Fact]
    public void Parse_WhenSevenFieldExpressionWithYearProvided_ReturnsCronExpression()
    {
        var expression = CronExpression.Parse("0 0 12 ? JAN MON#2 2028");

        Assert.True(expression.HasSeconds);
        Assert.True(expression.HasYear);
        Assert.Equal("0 0 12 ? 1 1#2 2028", expression.ToString());
        Assert.Equal(CronFieldElementType.Hash, expression.DayOfWeek.Items[0].Type);
        Assert.Equal(0, expression.Seconds.Items[0].Value);
        Assert.Equal(0, expression.Minutes.Items[0].Value);
    }

    [Fact]
    public void Parse_WhenSixFieldExpressionWithSecondsProvided_ReturnsCronExpression()
    {
        var expression = CronExpression.Parse("0 */5 * * * *");

        Assert.True(expression.HasSeconds);
        Assert.False(expression.HasYear);
        Assert.Equal("0 */5 * * * *", expression.ToString());
    }

    [Fact]
    public void Parse_WhenStepBaseIsInvalid_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CronExpression.Parse("0 L/2 * * ?"));
    }

    [Theory]
    [InlineData("0 0/ * * * *")]
    [InlineData("0 0/0 * * * *")]
    [InlineData("0 0/abc * * * *")]
    [InlineData("0 1/2/3 * * * *")]
    public void Parse_WhenStepSyntaxIsInvalid_ThrowsFormatException(string expression)
    {
        Assert.Throws<FormatException>(() => CronExpression.Parse(expression));
    }
    
    [Fact]
    public void Parse_WhenStepUsesRangeBase_ReturnsStepElementWithRangeBase()
    {
        var expression = CronExpression.Parse("0 1-10/2 * * * *");
        var minute = expression.Minutes.Items[0];

        Assert.Equal(CronFieldElementType.Step, minute.Type);
        Assert.Equal(2, minute.Step);
        Assert.Equal(CronFieldElementType.Range, minute.StepBase.Type);
        Assert.Equal(1, minute.StepBase.Start);
        Assert.Equal(10, minute.StepBase.End);
    }

    [Theory]
    [InlineData("@yearly", "0 0 1 1 *")]
    [InlineData("@annually", "0 0 1 1 *")]
    [InlineData("@monthly", "0 0 1 * *")]
    [InlineData("@weekly", "0 0 * * 0")]
    [InlineData("@daily", "0 0 * * *")]
    [InlineData("@midnight", "0 0 * * *")]
    [InlineData("@hourly", "0 * * * *")]
    public void Parse_WhenSupportedMacroProvided_ReturnsCanonicalExpression(string macro, string expected)
    {
        var expression = CronExpression.Parse(macro);

        Assert.Equal(expected, expression.ToString());
    }

    [Fact]
    public void Parse_WhenUnsupportedMacroProvided_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CronExpression.Parse("@foobar"));
    }

    [Theory]
    [InlineData("0 0 0 * *")]
    [InlineData("0 60 * * *")]
    [InlineData("0 0 32 * *")]
    [InlineData("0 0 * 13 *")]
    [InlineData("0 0 * * 8")]
    [InlineData("0 0 0 1 1 * 1969")]
    [InlineData("0 0 0 1 1 * 2200")]
    public void Parse_WhenValuesAreOutOfRange_ThrowsFormatException(string expression)
    {
        Assert.Throws<FormatException>(() => CronExpression.Parse(expression));
    }

    [Fact]
    public void ToString_WhenCultureUsesNonLatinDigits_UsesInvariantNumberFormat()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ar-SA");

            var expression = CronExpression.Parse("0 */5 * * * *");

            Assert.Equal("0 */5 * * * *", expression.ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Theory]
    [InlineData("* * * * *", true)]
    [InlineData("0 */5 * * * *", true)]
    [InlineData("0 0 1 * ?", true)]
    [InlineData("0 0 0 1 1 ? 2026", true)]
    [InlineData("@daily", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("99 * * * *", false)]
    [InlineData("0 0/0 * * * *", false)]
    [InlineData("@unknown", false)]
    [InlineData("0 0 0 1 1 ? 2200", false)]
    public void TryParse_WhenCalledWithRepresentativeInputs_ReturnsExpectedResult(string expressionText, bool expectedSuccess)
    {
        var success = CronExpression.TryParse(expressionText!, out var expression);

        Assert.Equal(expectedSuccess, success);

        if (expectedSuccess)
        {
            Assert.NotNull(expression);
        }
        else
        {
            Assert.Null(expression);
        }
    }
}
