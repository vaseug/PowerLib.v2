using PowerLib.System.ComponentModel;

namespace PowerLib.System;

/// <summary>
/// Comparision criteria
/// </summary>
public enum ComparisonCriteria
{
  [DisplayStringResource(typeof(ComparisonCriteria), nameof(Equal))]
  Equal,
  [DisplayStringResource(typeof(ComparisonCriteria), nameof(NotEqual))]
  NotEqual,
  [DisplayStringResource(typeof(ComparisonCriteria), nameof(LessThan))]
  LessThan,
  [DisplayStringResource(typeof(ComparisonCriteria), nameof(GreaterThan))]
  GreaterThan,
  [DisplayStringResource(typeof(ComparisonCriteria), nameof(LessThanOrEqual))]
  LessThanOrEqual,
  [DisplayStringResource(typeof(ComparisonCriteria), nameof(GreaterThanOrEqual))]
  GreaterThanOrEqual,
}