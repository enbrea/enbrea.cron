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
/// Specifies options for generating a human-readable description of a CRON expression.
/// </summary>
/// <remarks>
/// Use Language to control the output language and ReferenceTimeUtc to provide a UTC reference point for
/// time-sensitive descriptions.
/// </remarks>
public sealed class CronDescriptorOptions
{
    /// <summary>
    /// A two-letter ISO 639-1 language code (e.g., "en" for English, "de" for German) to specify the language of 
    /// the output description.
    /// </summary>
    public string Language { get; init; }

    /// <summary>
    /// Gets the reference point in Coordinated Universal Time (UTC).
    /// </summary>
    public DateTimeOffset? ReferenceTimeUtc { get; init; }
}
