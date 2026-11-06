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
/// A collection of cron field items that together define a single field within a cron expression.
/// </summary>
/// <remarks>
/// A cron field consists of one or more cron field items, such as values, ranges, or special symbols, 
/// which are used to specify the allowed values for a particular field (e.g., minute, hour, day) in 
/// a cron schedule. This class ensures that at least one item is present in the collection. The 
/// items are typically combined using a comma to form the complete field expression.
/// </remarks>
internal sealed class CronField
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CronField"/> class.
    /// </summary>
    /// <param name="items">A read-only list of <see cref="CronFieldElement"/> objects that define the 
    /// cron field expression.</param>
    internal CronField(IReadOnlyList<CronFieldElement> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            throw new ArgumentException("At least one cron field item is required.", nameof(items));
        }

        Items = Array.AsReadOnly(items.ToArray()); 
    }

    /// <summary>
    /// Gets a value indicating whether the field consists only of a wildcard item.
    /// </summary>
    internal bool IsAny => Items.Count == 1 && Items[0].Type == CronFieldElementType.Any;

    /// <summary>
    /// Gets a value indicating whether the field consists only of a question-mark item.
    /// </summary>
    internal bool IsQuestion => Items.Count == 1 && Items[0].Type == CronFieldElementType.Question;

    /// <summary>
    /// List of field items.
    /// </summary>
    internal IReadOnlyList<CronFieldElement> Items { get; }

    /// <summary>
    /// Returns a string that represents the current object by concatenating the string 
    /// representations of the items in the collection, separated by commas.
    /// </summary>
    /// <returns>A string representation of the cron field expression.</returns>
    public override string ToString()
    {
        return string.Join(",", Items);
    }

    /// <summary>
    /// Returns the first matching numeric value in the specified inclusive bounds.
    /// </summary>
    internal int GetFirstMatchingValue(int minimum, int maximum)
    {
        if (TryGetNextMatchingValue(minimum, minimum, maximum, out var value))
        {
            return value;
        }

        throw new InvalidOperationException("No matching value exists in the provided range.");
    }

    /// <summary>
    /// Returns the last matching numeric value in the specified inclusive bounds.
    /// </summary>
    internal int GetLastMatchingValue(int minimum, int maximum)
    {
        if (TryGetPreviousMatchingValue(maximum, minimum, maximum, out var value))
        {
            return value;
        }

        throw new InvalidOperationException("No matching value exists in the provided range.");
    }

    /// <summary>
    /// Determines whether a numeric value matches any ordinary item in this field within the 
    /// specified inclusive bounds.
    /// </summary>
    internal bool Matches(int value, int minimum, int maximum)
    {
        if (value < minimum || value > maximum)
        {
            return false;
        }

        var items = Items;

        for (var index = 0; index < items.Count; index++)
        {
            if (IsNumericItemMatch(items[index], value, minimum, maximum))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Tries to get the first matching value greater than or equal to <paramref name="current"/>.
    /// </summary>
    internal bool TryGetNextMatchingValue(int current, int minimum, int maximum, out int value)
    {
        value = default;

        var start = Math.Max(current, minimum);

        for (var candidate = start; candidate <= maximum; candidate++)
        {
            if (Matches(candidate, minimum, maximum))
            {
                value = candidate;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Tries to get the last matching value less than or equal to <paramref name="current"/>.
    /// </summary>
    internal bool TryGetPreviousMatchingValue(int current, int minimum, int maximum, out int value)
    {
        value = default;

        var start = Math.Min(current, maximum);

        for (var candidate = start; candidate >= minimum; candidate--)
        {
            if (Matches(candidate, minimum, maximum))
            {
                value = candidate;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether a value matches a single ordinary numeric field item.
    /// </summary>
    private static bool IsNumericItemMatch(CronFieldElement item, int value, int minimum, int maximum)
    {
        return item.Type switch
        {
            CronFieldElementType.Any => value >= minimum && value <= maximum,
            CronFieldElementType.Value => value == item.Value,
            CronFieldElementType.Range => value >= item.Start && value <= item.End,
            CronFieldElementType.Step => IsStepMatch(item, value, minimum, maximum),
            _ => false
        };
    }

    /// <summary>
    /// Determines whether a value matches an ordinary step field item.
    /// </summary>
    private static bool IsStepMatch(CronFieldElement item, int value, int minimum, int maximum)
    {
        if (item.Step <= 0 || item.StepBase == null)
        {
            return false;
        }

        if (item.StepBase.Type == CronFieldElementType.Any)
        {
            return value >= minimum
                && value <= maximum
                && ((value - minimum) % item.Step) == 0;
        }

        if (item.StepBase.Type == CronFieldElementType.Value)
        {
            var start = item.StepBase.Value;

            return value >= start
                && value <= maximum
                && ((value - start) % item.Step) == 0;
        }

        if (item.StepBase.Type == CronFieldElementType.Range)
        {
            var start = item.StepBase.Start;
            var end = item.StepBase.End;

            return value >= start
                && value <= end
                && ((value - start) % item.Step) == 0;
        }

        return false;
    }
}
