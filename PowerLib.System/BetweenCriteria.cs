using PowerLib.System.ComponentModel;

namespace PowerLib.System;

/// <summary>
/// Between criteria
/// </summary>
public enum BetweenCriteria
{
  [DisplayStringResource(typeof(BetweenCriteria), nameof(IncludeBoth))]
  IncludeBoth,
  [DisplayStringResource(typeof(BetweenCriteria), nameof(ExcludeLower))]
  ExcludeLower,
  [DisplayStringResource(typeof(BetweenCriteria), nameof(ExcludeUpper))]
  ExcludeUpper,
  [DisplayStringResource(typeof(BetweenCriteria), nameof(ExcludeBoth))]
  ExcludeBoth
}
