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
using System.Runtime.CompilerServices;

namespace Enbrea.Cron;

/// <summary>
/// Parses the five-, six-, and seven-field cron dialect supported by <c>Enbrea.Cron</c>.
/// </summary>
/// <remarks>
/// This parser offers static methods for parsing cron expressions with 5, 6, or 7 fields, as well 
/// as common macro expressions such as '@daily' or '@monthly'. This class is thread-safe.
/// </remarks>
internal static class CronParser
{
    /// <summary>
    /// Parses a cron expression string and returns a corresponding <see cref="CronExpression" /> 
    /// object.
    /// </summary>
    /// <param name="expressionText">The cron expression to parse.</param>
    /// <returns>A cron expression that represents the parsed cron expression.</returns>
    internal static CronExpression Parse(string expressionText)
    {
        // Validate input.
        ArgumentException.ThrowIfNullOrWhiteSpace(expressionText, nameof(expressionText));

        // Trim whitespace for consistent parsing.
        var trimmedExpression = expressionText.Trim();

        // Normalize macro aliases.
        var normalizedExpression = ExpandMacro(trimmedExpression);

        // Prepare span view for allocation-friendly parsing.
        var expressionSpan = normalizedExpression.AsSpan();

        // Pre-allocate storage for up to 7 cron fields.
        Span<int> fieldStarts = stackalloc int[7];
        Span<int> fieldLengths = stackalloc int[7];

        // Tokenize expression into field spans.
        var fieldCount = CollectFieldSpans(expressionSpan, fieldStarts, fieldLengths);

        // Parse fields according to 5/6/7-field cron format.
        var parsedExpression = fieldCount switch
        {
            5 => ParseFiveFieldExpression(expressionSpan, fieldStarts, fieldLengths),
            6 => ParseSixFieldExpression(expressionSpan, fieldStarts, fieldLengths),
            7 => ParseSevenFieldExpression(expressionSpan, fieldStarts, fieldLengths),
            _ => throw new FormatException("Cron expression must have 5, 6 or 7 fields.")
        };

        // Return the parsed expression.
        return parsedExpression;
    }

    /// <summary>
    /// Attempts to parse a cron expression string and returns a corresponding <see cref="CronExpression" /> object if
    /// successful.
    /// </summary>
    /// <param name="expressionText">The cron expression string to parse.</param>
    /// <param name="result">When this method returns, contains the parsed cron expression if the 
    /// parsing succeeded, or <c>null</c> if the parsing failed.</param>
    /// <returns><c>true</c> if the parsing succeeded; otherwise, <c>false</c>.</returns>
    internal static bool TryParse(string expressionText, out CronExpression result)
    {
        try
        {
            result = Parse(expressionText);
            return true;

        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Collects the starting indices and lengths of each field in a cron expression span, storing 
    /// them in the provided spans.
    /// </summary>
    private static int CollectFieldSpans(ReadOnlySpan<char> expressionSpan, Span<int> fieldStarts, Span<int> fieldLengths)
    {
        var fieldCount = 0;
        var index = 0;

        while (index < expressionSpan.Length)
        {
            while (index < expressionSpan.Length && char.IsWhiteSpace(expressionSpan[index]))
            {
                index++;
            }

            if (index >= expressionSpan.Length)
            {
                break;
            }

            if (fieldCount == fieldStarts.Length)
            {
                throw new FormatException("Cron expression must have 5, 6 or 7 fields.");
            }

            var start = index;
            while (index < expressionSpan.Length && !char.IsWhiteSpace(expressionSpan[index]))
            {
                index++;
            }

            fieldStarts[fieldCount] = start;
            fieldLengths[fieldCount] = index - start;
            fieldCount++;
        }

        return fieldCount;
    }

    /// <summary>
    /// Expands a cron macro expression to its equivalent standard cron format string.
    /// </summary>
    private static string ExpandMacro(string expression)
    {
        if (!expression.StartsWith('@'))
        {
            return expression;
        }

        return expression.ToLowerInvariant() switch
        {
            "@yearly" => "0 0 1 1 *",
            "@annually" => "0 0 1 1 *",
            "@monthly" => "0 0 1 * *",
            "@weekly" => "0 0 * * 0",
            "@daily" => "0 0 * * *",
            "@midnight" => "0 0 * * *",
            "@hourly" => "0 * * * *",
            _ => throw new FormatException($"Unsupported cron macro '{expression}'.")
        };
    }

    /// <summary>
    /// Returns the minimum and maximum valid values for the specified cron field type.
    /// </summary>
    private static (int Min, int Max) GetBounds(CronFieldType field)
    {
        return field switch
        {
            CronFieldType.Seconds => (0, 59),
            CronFieldType.Minutes => (0, 59),
            CronFieldType.Hours => (0, 23),
            CronFieldType.DayOfMonth => (1, 31),
            CronFieldType.Month => (1, 12),
            CronFieldType.DayOfWeek => (0, 7),
            CronFieldType.Year => (CronYearBounds.MinYear, CronYearBounds.MaxYear),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unsupported cron field.")
        };
    }

    /// <summary>
    /// Returns a span representing the field at the specified index within the given expression 
    /// span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<char> GetFieldSpan(
        ReadOnlySpan<char> expressionSpan, 
        ReadOnlySpan<int> fieldStarts, 
        ReadOnlySpan<int> fieldLengths, int index)
    {
        return expressionSpan.Slice(fieldStarts[index], fieldLengths[index]);
    }

    /// <summary>
    /// Parses a single cron field atom and returns the corresponding cron field element 
    /// representation.
    /// </summary>
    private static CronFieldElement ParseAtom(ReadOnlySpan<char> atom, CronFieldType field)
    {
        if (field == CronFieldType.DayOfMonth)
        {
            if (atom.Equals("L", StringComparison.OrdinalIgnoreCase))
            {
                return CronFieldElement.Last();
            }

            if (atom.Equals("LW", StringComparison.OrdinalIgnoreCase))
            {
                return CronFieldElement.LastWeekday();
            }

            if (atom.Length > 1 && atom.EndsWith("W", StringComparison.OrdinalIgnoreCase))
            {
                var dayPortion = atom[..^1];
                return CronFieldElement.NearestWeekday(ParseValue(dayPortion, field));
            }
        }

        if (field == CronFieldType.DayOfWeek)
        {
            if (atom.Length > 1 && atom.EndsWith("L", StringComparison.OrdinalIgnoreCase))
            {
                return CronFieldElement.LastDayOfWeek(ParseValue(atom[..^1], field));
            }

            var hashIndex = atom.IndexOf('#');
            if (hashIndex >= 0)
            {
                if (hashIndex != atom.LastIndexOf('#'))
                {
                    throw new FormatException($"Field '{field}' has invalid '#'-syntax '{atom}'.");
                }

                var day = ParseValue(atom[..hashIndex], field);
                var occurrenceText = atom[(hashIndex + 1)..];
                if (!int.TryParse(occurrenceText, NumberStyles.None, CultureInfo.InvariantCulture, out var occurrence) || occurrence < 1 || occurrence > 5)
                {
                    throw new FormatException($"Field '{field}' has invalid '#'-occurrence in '{atom}'.");
                }

                return CronFieldElement.Hash(day, occurrence);
            }
        }

        var rangeIndex = atom.IndexOf('-');
        if (rangeIndex >= 0)
        {
            if (rangeIndex != atom.LastIndexOf('-'))
            {
                throw new FormatException($"Field '{field}' has invalid range syntax '{atom}'.");
            }

            var start = ParseValue(atom[..rangeIndex], field);
            var end = ParseValue(atom[(rangeIndex + 1)..], field);

            if (start > end && field != CronFieldType.DayOfWeek)
            {
                throw new FormatException($"Field '{field}' has descending range '{atom}'.");
            }

            return CronFieldElement.Range(start, end);
        }

        return CronFieldElement.ValueItem(ParseValue(atom, field));
    }

    /// <summary>
    /// Parses a single cron field value and returns a corresponding cron field representation.
    /// </summary>
    private static CronField ParseField(ReadOnlySpan<char> fieldValue, CronFieldType field)
    {
        if (fieldValue.IsEmpty)
        {
            throw new FormatException($"Field '{field}' cannot be empty.");
        }

        List<CronFieldElement> parsedItems = null;
        var hasQuestion = false;
        var partStart = 0;

        for (var index = 0; index <= fieldValue.Length; index++)
        {
            var isEnd = index == fieldValue.Length;
            if (!isEnd && fieldValue[index] != ',')
            {
                continue;
            }

            var part = fieldValue[partStart..index].Trim();
            if (part.IsEmpty)
            {
                throw new FormatException($"Field '{field}' is invalid.");
            }

            var parsedItem = ParsePart(part, field);
            ValidateQuestionUsage(field, parsedItem, parsedItems, ref hasQuestion);

            parsedItems ??= new List<CronFieldElement>(4);
            parsedItems.Add(parsedItem);
            partStart = index + 1;
        }

        return new CronField(parsedItems ?? throw new FormatException($"Field '{field}' is invalid."));
    }

    /// <summary>
    /// Parses a cron expression with five fields (minute, hour, day of month, month, day of week) 
    /// and returns a corresponding cron expression representation.
    /// </summary>
    private static CronExpression ParseFiveFieldExpression(
        ReadOnlySpan<char> expressionSpan, 
        ReadOnlySpan<int> fieldStarts, 
        ReadOnlySpan<int> fieldLengths)
    {
        var minutes = ParseField(GetFieldSpan(expressionSpan, fieldStarts, fieldLengths, 0), CronFieldType.Minutes);
        var hours = ParseField(GetFieldSpan(expressionSpan, fieldStarts, fieldLengths, 1), CronFieldType.Hours);
        var dayOfMonth = ParseField(GetFieldSpan(expressionSpan, fieldStarts, fieldLengths, 2), CronFieldType.DayOfMonth);
        var months = ParseField(GetFieldSpan(expressionSpan, fieldStarts, fieldLengths, 3), CronFieldType.Month);
        var dayOfWeek = ParseField(GetFieldSpan(expressionSpan, fieldStarts, fieldLengths, 4), CronFieldType.DayOfWeek);

        ValidateDayCombination(dayOfMonth, dayOfWeek);

        return new CronExpression(minutes, hours, dayOfMonth, months, dayOfWeek);
    }

    /// <summary>
    /// Parses a single cron field part and returns the corresponding cron field element 
    /// representation.
    /// </summary>
    private static CronFieldElement ParsePart(ReadOnlySpan<char> part, CronFieldType field)
    {
        if (part.Equals("*", StringComparison.Ordinal))
        {
            return CronFieldElement.Any();
        }

        if (part.Equals("?", StringComparison.Ordinal))
        {
            if (field is not (CronFieldType.DayOfMonth or CronFieldType.DayOfWeek))
            {
                throw new FormatException($"Field '{field}' does not support '?'.");
            }

            return CronFieldElement.Question();
        }

        var stepIndex = part.IndexOf('/');
        if (stepIndex >= 0)
        {
            if (stepIndex != part.LastIndexOf('/'))
            {
                throw new FormatException($"Field '{field}' has invalid step syntax '{part}'.");
            }

            var basePart = part[..stepIndex].Trim();
            var stepPart = part[(stepIndex + 1)..].Trim();

            if (!int.TryParse(stepPart, NumberStyles.None, CultureInfo.InvariantCulture, out var step) || step <= 0)
            {
                throw new FormatException($"Field '{field}' has invalid step value '{stepPart}'.");
            }

            var parsedBase = ParsePart(basePart, field);
            if (parsedBase.Type is not (CronFieldElementType.Any or CronFieldElementType.Value or CronFieldElementType.Range))
            {
                throw new FormatException($"Field '{field}' has unsupported step base '{basePart}'.");
            }

            return CronFieldElement.StepItem(parsedBase, step);
        }

        return ParseAtom(part, field);
    }

    /// <summary>
    /// Parses a seven-field cron expression from the specified character span and field boundaries.
    /// </summary>
    private static CronExpression ParseSevenFieldExpression(
        ReadOnlySpan<char> expressionSpan, 
        ReadOnlySpan<int> fieldStarts, 
        ReadOnlySpan<int> fieldLengths)
    {
        var expressionSeconds = ParseField(GetFieldSpan(expressionSpan, fieldStarts, fieldLengths, 0), CronFieldType.Seconds);
        var expressionMinutes = ParseField(GetFieldSpan(expressionSpan, fieldStarts, fieldLengths, 1), CronFieldType.Minutes);
        var expressionHours = ParseField(GetFieldSpan(expressionSpan, fieldStarts, fieldLengths, 2), CronFieldType.Hours);
        var expressionDayOfMonth = ParseField(GetFieldSpan(expressionSpan, fieldStarts, fieldLengths, 3), CronFieldType.DayOfMonth);
        var expressionMonths = ParseField(GetFieldSpan(expressionSpan, fieldStarts, fieldLengths, 4), CronFieldType.Month);
        var expressionDayOfWeek = ParseField(GetFieldSpan(expressionSpan, fieldStarts, fieldLengths, 5), CronFieldType.DayOfWeek);
        var years = ParseField(GetFieldSpan(expressionSpan, fieldStarts, fieldLengths, 6), CronFieldType.Year);

        ValidateDayCombination(expressionDayOfMonth, expressionDayOfWeek);

        return new CronExpression(expressionSeconds, expressionMinutes, expressionHours, expressionDayOfMonth, expressionMonths, expressionDayOfWeek, years);
    }

    /// <summary>
    /// Parses a six-field cron expression from the specified character span and field boundaries.
    /// </summary>
    private static CronExpression ParseSixFieldExpression(
        ReadOnlySpan<char> expressionSpan, 
        ReadOnlySpan<int> fieldStarts, 
        ReadOnlySpan<int> fieldLengths)
    {
        var seconds = ParseField(GetFieldSpan(expressionSpan, fieldStarts, fieldLengths, 0), CronFieldType.Seconds);
        var minutes = ParseField(GetFieldSpan(expressionSpan, fieldStarts, fieldLengths, 1), CronFieldType.Minutes);
        var hours = ParseField(GetFieldSpan(expressionSpan, fieldStarts, fieldLengths, 2), CronFieldType.Hours);
        var dayOfMonth = ParseField(GetFieldSpan(expressionSpan, fieldStarts, fieldLengths, 3), CronFieldType.DayOfMonth);
        var months = ParseField(GetFieldSpan(expressionSpan, fieldStarts, fieldLengths, 4), CronFieldType.Month);
        var dayOfWeek = ParseField(GetFieldSpan(expressionSpan, fieldStarts, fieldLengths, 5), CronFieldType.DayOfWeek);

        ValidateDayCombination(dayOfMonth, dayOfWeek);

        return new CronExpression(seconds, minutes, hours, dayOfMonth, months, dayOfWeek);
    }
    
    /// <summary>
    /// Parses a cron field value from the specified character span, interpreting named values and 
    /// validating numeric ranges as appropriate for the given field type.
    /// </summary>
    private static int ParseValue(ReadOnlySpan<char> value, CronFieldType field)
    {
        if (value.IsEmpty)
        {
            throw new FormatException($"Field '{field}' contains an empty value.");
        }

        if (TryParseNamedValue(value, field, out var namedValue))
        {
            return namedValue;
        }

        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var numericValue))
        {
            throw new FormatException($"Field '{field}' contains invalid value '{value}'.");
        }

        var (min, max) = GetBounds(field);
        if (numericValue < min || numericValue > max)
        {
            throw new FormatException($"Field '{field}' value '{value}' is outside of the supported range {min}-{max}.");
        }

        return numericValue;
    }

    /// <summary>
    /// Attempts to parse a three-letter English day-of-week abbreviation and returns the 
    /// corresponding day number.
    /// </summary>
    private static bool TryParseDayOfWeekName(ReadOnlySpan<char> value, out int day)
    {
        if (value.Equals("SUN", StringComparison.OrdinalIgnoreCase)) { day = 0; return true; }
        if (value.Equals("MON", StringComparison.OrdinalIgnoreCase)) { day = 1; return true; }
        if (value.Equals("TUE", StringComparison.OrdinalIgnoreCase)) { day = 2; return true; }
        if (value.Equals("WED", StringComparison.OrdinalIgnoreCase)) { day = 3; return true; }
        if (value.Equals("THU", StringComparison.OrdinalIgnoreCase)) { day = 4; return true; }
        if (value.Equals("FRI", StringComparison.OrdinalIgnoreCase)) { day = 5; return true; }
        if (value.Equals("SAT", StringComparison.OrdinalIgnoreCase)) { day = 6; return true; }

        day = 0;
        return false;
    }

    /// <summary>
    /// Attempts to parse a three-letter English month abbreviation from the specified span and 
    /// returns its corresponding month number.
    /// </summary>
    private static bool TryParseMonthName(ReadOnlySpan<char> value, out int month)
    {
        if (value.Equals("JAN", StringComparison.OrdinalIgnoreCase)) { month = 1; return true; }
        if (value.Equals("FEB", StringComparison.OrdinalIgnoreCase)) { month = 2; return true; }
        if (value.Equals("MAR", StringComparison.OrdinalIgnoreCase)) { month = 3; return true; }
        if (value.Equals("APR", StringComparison.OrdinalIgnoreCase)) { month = 4; return true; }
        if (value.Equals("MAY", StringComparison.OrdinalIgnoreCase)) { month = 5; return true; }
        if (value.Equals("JUN", StringComparison.OrdinalIgnoreCase)) { month = 6; return true; }
        if (value.Equals("JUL", StringComparison.OrdinalIgnoreCase)) { month = 7; return true; }
        if (value.Equals("AUG", StringComparison.OrdinalIgnoreCase)) { month = 8; return true; }
        if (value.Equals("SEP", StringComparison.OrdinalIgnoreCase)) { month = 9; return true; }
        if (value.Equals("OCT", StringComparison.OrdinalIgnoreCase)) { month = 10; return true; }
        if (value.Equals("NOV", StringComparison.OrdinalIgnoreCase)) { month = 11; return true; }
        if (value.Equals("DEC", StringComparison.OrdinalIgnoreCase)) { month = 12; return true; }

        month = 0;
        return false;
    }

    /// <summary>
    /// Attempts to parse a named value from the specified character span according to the given 
    /// cron field type.
    /// </summary>
    private static bool TryParseNamedValue(ReadOnlySpan<char> value, CronFieldType field, out int namedValue)
    {
        switch (field)
        {
            case CronFieldType.Month:
                return TryParseMonthName(value, out namedValue);
            case CronFieldType.DayOfWeek:
                return TryParseDayOfWeekName(value, out namedValue);
            default:
                namedValue = 0;
                return false;
        }
    }

    /// <summary>
    /// Validates that the combination of day-of-month and day-of-week fields in a cron expression 
    /// does not specify both as '?'.
    /// </summary>
    private static void ValidateDayCombination(CronField dayOfMonth, CronField dayOfWeek)
    {
        var dayOfMonthIsQuestion = dayOfMonth.IsQuestion;
        var dayOfWeekIsQuestion = dayOfWeek.IsQuestion;

        if (dayOfMonthIsQuestion && dayOfWeekIsQuestion)
        {
            throw new FormatException("Day-of-month and day-of-week cannot both be '?'.");
        }
    }

    /// <summary>
    /// Validates the usage of the question mark ('?') character within a cron field, ensuring it 
    /// appears only in valid contexts according to cron expression rules.
    /// </summary>
    private static void ValidateQuestionUsage(CronFieldType field, CronFieldElement parsedItem, List<CronFieldElement> parsedItems, ref bool hasQuestion)
    {
        if (parsedItem.Type == CronFieldElementType.Question)
        {
            if (hasQuestion || parsedItems is { Count: > 0 })
            {
                throw new FormatException($"Field '{field}' contains an invalid '?'.");
            }

            hasQuestion = true;
            return;
        }

        if (hasQuestion)
        {
            throw new FormatException($"Field '{field}' contains an invalid '?'.");
        }
    }
}
