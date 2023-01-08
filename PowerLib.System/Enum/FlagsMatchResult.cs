using PowerLib.System.ComponentModel;

namespace PowerLib.System;

/// <summary>
/// Enumeartion flags values match result
/// </summary>
public enum FlagsMatchResult
{
    /// <summary>
    /// Nonoverlaped values
    /// </summary>
    [DisplayStringResource(typeof(FlagsMatchResult), nameof(NonOverlap))]
    NonOverlap,
    /// <summary>
    /// Overlaped values
    /// </summary>
    [DisplayStringResource(typeof(FlagsMatchResult), nameof(Overlap))]
    Overlap,
    /// <summary>
    /// Equal values
    /// </summary>
    [DisplayStringResource(typeof(FlagsMatchResult), nameof(Equal))]
    Equal,
    /// <summary>
    /// First value belong to second
    /// </summary>
    [DisplayStringResource(typeof(FlagsMatchResult), nameof(Belong))]
    Belong,
    /// <summary>
    /// First value enclosed by second
    /// </summary>
    [DisplayStringResource(typeof(FlagsMatchResult), nameof(Enclose))]
    Enclose
}
