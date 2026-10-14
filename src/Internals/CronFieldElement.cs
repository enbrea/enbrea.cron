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

namespace Enbrea.Cron;

/// <summary>
/// A single element within a cron field, such as a specific value, range, step, or special modifier, used to define 
/// scheduling rules in a cron expression.
/// </summary>
internal sealed class CronFieldElement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CronFieldElement"/> class.
    /// </summary>
    private CronFieldElement(CronFieldElementType type)
    {
        Type = type;
    }

    /// <summary>
    /// Gets the range end for <see cref="CronFieldElementType.Range"/>.
    /// </summary>
    internal int End { get; init; }

    /// <summary>
    /// Gets the occurrence index for <see cref="CronFieldElementType.Hash"/>.
    /// </summary>
    internal int Occurrence { get; init; }

    /// <summary>
    /// Gets the range start for <see cref="CronFieldElementType.Range"/>.
    /// </summary>
    internal int Start { get; init; }

    /// <summary>
    /// Gets the step value for <see cref="CronFieldElementType.Step"/>.
    /// </summary>
    internal int Step { get; init; }

    /// <summary>
    /// Gets the base expression for <see cref="CronFieldElementType.Step"/>.
    /// </summary>
    internal CronFieldElement StepBase { get; init; }

    /// <summary>
    /// Gets the element type.
    /// </summary>
    internal CronFieldElementType Type { get; }

    /// <summary>
    /// Gets the numeric operand for value-based and special field elements.
    /// </summary>
    internal int Value { get; init; }

    /// <summary>
    /// Returns a string that represents the current cron field element in standard cron expression format.
    /// </summary>
    /// <returns>A string representation of the cron field element, formatted according to its type.</returns>
    public override string ToString()
    {
        return Type switch
        {
            CronFieldElementType.Any => "*",
            CronFieldElementType.Value => Value.ToString(CultureInfo.InvariantCulture),
            CronFieldElementType.Range => $"{Start.ToString(CultureInfo.InvariantCulture)}-{End.ToString(CultureInfo.InvariantCulture)}",
            CronFieldElementType.Step => $"{StepBase}/{Step}",
            CronFieldElementType.Question => "?",
            CronFieldElementType.Last => "L",
            CronFieldElementType.LastWeekday => "LW",
            CronFieldElementType.NearestWeekday => $"{Value.ToString(CultureInfo.InvariantCulture)}W",
            CronFieldElementType.LastDayOfWeek => $"{Value.ToString(CultureInfo.InvariantCulture)}L",
            CronFieldElementType.Hash => $"{Value.ToString(CultureInfo.InvariantCulture)}#{Occurrence.ToString(CultureInfo.InvariantCulture)}",
            _ => throw new InvalidOperationException($"Unsupported cron field element type '{Type}'.")
        };
    }

    /// <summary>
    /// Creates an asterisk item.
    /// </summary>
    /// <returns>A cron field element.</returns>
    internal static CronFieldElement Any()
    {
        return new CronFieldElement(CronFieldElementType.Any);
    }

    /// <summary>
    /// Creates a hash item.
    /// </summary>
    /// <param name="value">The day-of-week value.</param>
    /// <param name="occurrence">The nth occurrence value.</param>
    /// <returns>A cron field element.</returns>
    internal static CronFieldElement Hash(int value, int occurrence)
    {
        return new CronFieldElement(CronFieldElementType.Hash) { Value = value, Occurrence = occurrence };
    }

    /// <summary>
    /// Creates a last-day item.
    /// </summary>
    /// <returns>A cron field element.</returns>
    internal static CronFieldElement Last()
    {
        return new CronFieldElement(CronFieldElementType.Last);
    }

    /// <summary>
    /// Creates a last-day-of-week item.
    /// </summary>
    /// <param name="value">The day-of-week value.</param>
    /// <returns>A cron field element.</returns>
    internal static CronFieldElement LastDayOfWeek(int value)
    {
        return new CronFieldElement(CronFieldElementType.LastDayOfWeek) { Value = value };
    }

    /// <summary>
    /// Creates a last-weekday item.
    /// </summary>
    /// <returns>A cron field element.</returns>
    internal static CronFieldElement LastWeekday()
    {
        return new CronFieldElement(CronFieldElementType.LastWeekday);
    }

    /// <summary>
    /// Creates a nearest-weekday item.
    /// </summary>
    /// <param name="value">The day value.</param>
    /// <returns>A cron field element.</returns>
    internal static CronFieldElement NearestWeekday(int value)
    {
        return new CronFieldElement(CronFieldElementType.NearestWeekday) { Value = value };
    }

    /// <summary>
    /// Creates a question-mark item.
    /// </summary>
    /// <returns>A cron field element.</returns>
    internal static CronFieldElement Question()
    {
        return new CronFieldElement(CronFieldElementType.Question);
    }

    /// <summary>
    /// Creates a range item.
    /// </summary>
    /// <param name="start">The start value.</param>
    /// <param name="end">The end value.</param>
    /// <returns>A cron field element.</returns>
    internal static CronFieldElement Range(int start, int end)
    {
        return new CronFieldElement(CronFieldElementType.Range) { Start = start, End = end };
    }

    /// <summary>
    /// Creates a step item.
    /// </summary>
    /// <param name="stepBase">The step base item.</param>
    /// <param name="step">The step value.</param>
    /// <returns>A cron field element.</returns>
    internal static CronFieldElement StepItem(CronFieldElement stepBase, int step)
    {
        return new CronFieldElement(CronFieldElementType.Step) { StepBase = stepBase, Step = step };
    }

    /// <summary>
    /// Creates a value item.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A cron field element.</returns>
    internal static CronFieldElement ValueItem(int value)
    {
        return new CronFieldElement(CronFieldElementType.Value) { Value = value };
    }
}
