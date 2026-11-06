#region Enbrea.Cron - Copyright (c) STÜBER SYSTEMS GmbH
/*    
 *    Enbrea.Cron 
 *    
 *    Copyright (c) STÜBER SYSTEMS GmbH
 *
 *    Licensed under the MIT License.
 */
#endregion

using Enbrea.Cron.Descriptor;
using Xunit;

namespace Enbrea.Cron.Tests;

public sealed class CronDescriptorTests
{
    [Fact]
    public void Describe_WhenExpressionIsDailyInEnglish_ReturnsExpectedText()
    {
        var expression = CronExpression.Parse("30 8 * * *");

        Assert.Equal("every day at 08:30", expression.Describe("en"));
    }

    [Theory]
    [InlineData("fr", "chaque jour à 08:30")]
    [InlineData("it", "ogni giorno alle 08:30")]
    [InlineData("es", "cada día a las 08:30")]
    [InlineData("pt", "todos os dias às 08:30")]
    [InlineData("tr", "her gün saat 08:30")]
    [InlineData("ru", "каждый день в 08:30")]
    [InlineData("uk", "щодня о 08:30")]
    public void Describe_WhenExpressionIsDailyInSupportedLanguage_ReturnsExpectedText(string language, string expected)
    {
        var expression = CronExpression.Parse("30 8 * * *");

        Assert.Equal(expected, expression.Describe(language));
    }

    [Fact]
    public void Describe_WhenExpressionIsHourlyInEnglish_ReturnsExpectedText()
    {
        var expression = CronExpression.Parse("0 * * * *");

        Assert.Equal("every hour at minute 0", expression.Describe("en"));
    }

    [Fact]
    public void Describe_WhenExpressionIsMinutelyInEnglish_ReturnsExpectedText()
    {
        var expression = CronExpression.Parse("* * * * *");

        Assert.Equal("every minute", expression.Describe("en"));
    }

    [Fact]
    public void Describe_WhenExpressionIsMinutelyInGerman_ReturnsExpectedText()
    {
        var expression = CronExpression.Parse("* * * * *");

        Assert.Equal("jede Minute", expression.Describe("de"));
    }

    [Theory]
    [InlineData("fr", "chaque minute")]
    [InlineData("it", "ogni minuto")]
    [InlineData("es", "cada minuto")]
    [InlineData("pt", "a cada minuto")]
    [InlineData("tr", "her dakika")]
    [InlineData("ru", "каждую минуту")]
    [InlineData("uk", "щохвилини")]
    public void Describe_WhenExpressionIsMinutelyInSupportedLanguage_ReturnsExpectedText(string language, string expected)
    {
        var expression = CronExpression.Parse("* * * * *");

        Assert.Equal(expected, expression.Describe(language));
    }

    [Fact]
    public void Describe_WhenExpressionIsMonthlyInEnglish_ReturnsExpectedText()
    {
        var expression = CronExpression.Parse("0 6 15 * *");

        Assert.Equal("every month on day 15 at 06:00", expression.Describe("en"));
    }

    [Fact]
    public void Describe_WhenExpressionIsWeekdaysInEnglish_ReturnsExpectedText()
    {
        var expression = CronExpression.Parse("0 8 * * 1-5");

        Assert.Equal("every weekday at 08:00", expression.Describe("en"));
    }

    [Fact]
    public void Describe_WhenExpressionIsWeekdaysInGerman_ReturnsExpectedText()
    {
        var expression = CronExpression.Parse("0 8 * * 1-5");

        Assert.Equal("jeden Werktag um 08:00", expression.Describe("de"));
    }

    [Theory]
    [InlineData("fr", "chaque jour de semaine à 08:00")]
    [InlineData("it", "ogni giorno feriale alle 08:00")]
    [InlineData("es", "cada día laborable a las 08:00")]
    [InlineData("pt", "todo dia útil às 08:00")]
    [InlineData("tr", "her hafta içi gün saat 08:00")]
    [InlineData("ru", "каждый будний день в 08:00")]
    [InlineData("uk", "щодня у будній день о 08:00")]
    public void Describe_WhenExpressionIsWeekdaysInSupportedLanguage_ReturnsExpectedText(string language, string expected)
    {
        var expression = CronExpression.Parse("0 8 * * 1-5");

        Assert.Equal(expected, expression.Describe(language));
    }

    [Fact]
    public void Describe_WhenExpressionIsWeekendsInEnglish_ReturnsExpectedText()
    {
        var expression = CronExpression.Parse("0 10 * * 6,7");

        Assert.Equal("every weekend day at 10:00", expression.Describe("en"));
    }

    [Fact]
    public void Describe_WhenExpressionIsWeeklyInEnglish_ReturnsExpectedText()
    {
        var expression = CronExpression.Parse("0 9 * * 1");

        Assert.Equal("every week on Monday at 09:00", expression.Describe("en"));
    }

    [Fact]
    public void Describe_WhenExpressionIsWeeklyInGerman_ReturnsExpectedText()
    {
        var expression = CronExpression.Parse("0 9 * * 1");

        Assert.Equal("jede Woche am Montag um 09:00", expression.Describe("de"));
    }

    [Fact]
    public void Describe_WhenExpressionIsYearlyInEnglish_ReturnsExpectedText()
    {
        var expression = CronExpression.Parse("15 6 24 12 *");

        Assert.Equal("every year on December 24 at 06:15", expression.Describe("en"));
    }

    [Fact]
    public void Describe_WhenExpressionIsYearlyInGerman_ReturnsExpectedText()
    {
        var expression = CronExpression.Parse("15 6 24 12 *");

        Assert.Equal("jedes Jahr am 24. Dezember um 06:15", expression.Describe("de"));
    }

    [Fact]
    public void Describe_WhenLanguageIsUnknown_FallsBackToEnglish()
    {
        var expression = CronExpression.Parse("* * * * *");

        Assert.Equal("every minute", expression.Describe("xx"));
    }

    [Fact]
    public void Describe_WhenOptionsAreNull_UsesDefaults()
    {
        var expression = CronExpression.Parse("* * * * *");
        var text = expression.Describe();

        Assert.NotNull(text);
        Assert.NotEmpty(text);
    }

    [Fact]
    public void Describe_WhenScheduleIsHourlyInEnglish_ReturnsRelativePhrase()
    {
        var schedule = CronSchedule.Parse("0 * * * *", TimeZoneInfo.Utc);
        var text = schedule.Describe(new CronDescriptorOptions
        {
            Language = "en",
            ReferenceTimeUtc = new DateTimeOffset(2026, 1, 1, 10, 30, 0, TimeSpan.Zero)
        });

        Assert.Equal("every hour at minute 0. Time zone: UTC. next occurrence in 30 minute(s). previous occurrence 30 minute(s) ago.", text);
    }

    [Fact]
    public void Describe_WhenScheduleIsHourlyInGerman_ReturnsRelativePhrase()
    {
        var schedule = CronSchedule.Parse("0 * * * *", TimeZoneInfo.Utc);
        var text = schedule.Describe(new CronDescriptorOptions
        {
            Language = "de",
            ReferenceTimeUtc = new DateTimeOffset(2026, 1, 1, 10, 30, 0, TimeSpan.Zero)
        });

        Assert.Equal("jede Stunde in Minute 0. Zeitzone: UTC. nächstes Auftreten in 30 Minute(n). vorheriges Auftreten vor 30 Minute(n).", text);
    }
}
