#region Enbrea.Cron - Copyright (c) STÜBER SYSTEMS GmbH
/*    
 *    Enbrea.Cron 
 *    
 *    Copyright (c) STÜBER SYSTEMS GmbH
 *
 *    Licensed under the MIT License.
 */
#endregion

namespace Enbrea.Cron.Descriptor;

/// <summary>
/// Provides extension methods for producing human-readable descriptions of cron expressions.
/// </summary>
public static class CronExpressionDescriptorExtensions
{
    /// <summary>
    /// Generates a human-readable description of a cron expression in the specified language.
    /// </summary>
    /// <param name="expression">The cron expression to describe.</param>
    /// <param name="language">The language code used to localize the description.</param>
    /// <returns>A human-readable description of the specified cron expression.</returns>
    public static string Describe(this CronExpression expression, string language)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return CronExpressionFormatter.Format(expression, new CronDescriptorOptions { Language = language });
    }

    /// <summary>
    /// Generates a human-readable description of a cron expression.    
    /// </summary>
    /// <param name="expression">The cron expression to describe.</param>
    /// <param name="options">Formatting options for the generated description. If <c>null</c>, default options are used.</param>
    /// <returns>A human-readable description of the specified cron expression.</returns>
    public static string Describe(this CronExpression expression, CronDescriptorOptions options = null)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return CronExpressionFormatter.Format(expression, options ?? new CronDescriptorOptions());
    }
}
